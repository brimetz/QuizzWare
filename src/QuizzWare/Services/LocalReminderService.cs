#if ANDROID || IOS
using Plugin.LocalNotification;
using Plugin.LocalNotification.Core.Models;
using Plugin.LocalNotification.Core.Models.AndroidOption;
using QuizzWare.Core.Progress;

namespace QuizzWare.Services;

/// <summary>Rappels via les notifications locales d'Android et d'iOS (Plugin.LocalNotification).</summary>
public sealed class LocalReminderService : IReminderService
{
    public bool IsSupported => true;

    public async Task<bool> RequestPermissionAsync()
    {
        var center = LocalNotificationCenter.Current;
        if (await center.AreNotificationsEnabled())
            return true;

        // Un rappel quotidien n'a pas besoin d'être à la seconde près : pas d'alarme « exacte » (permission spéciale sur Android 12+).
        return await center.RequestNotificationPermission(new NotificationPermission
        {
            Android = new AndroidNotificationPermission { RequestPermissionToScheduleExactAlarm = false },
        });
    }

    public async Task ScheduleAsync(IReadOnlyList<PlannedReminder> reminders)
    {
        var center = LocalNotificationCenter.Current;
        center.Cancel(ReminderPlanner.AllIds.ToArray());

        if (reminders.Count == 0 || !await center.AreNotificationsEnabled())
            return;

        foreach (var reminder in reminders)
            await ShowAsync(center, reminder);
    }

    public async Task ScheduleOneAsync(PlannedReminder reminder)
    {
        var center = LocalNotificationCenter.Current;
        if (await center.AreNotificationsEnabled())
            await ShowAsync(center, reminder);
    }

    private static Task<bool> ShowAsync(INotificationService center, PlannedReminder reminder) =>
        center.Show(new NotificationRequest
        {
            NotificationId = reminder.Id,
            Title = reminder.Title,
            Description = reminder.Message,
            Schedule = new NotificationRequestSchedule
            {
                NotifyTime = reminder.When,
                Android = new AndroidScheduleOptions
                {
                    // Réveille le téléphone s'il est en veille, y compris en veille profonde (Doze). Pas d'alarme exacte :
                    // elle demande une autorisation spéciale (réveils, agendas) ; Android peut donc décaler un peu le rappel.
                    AlarmType = AndroidAlarmType.RtcWakeup,
                    ScheduleMode = AndroidScheduleMode.InexactAllowWhileIdle,
                },
            },
        });
}
#endif
