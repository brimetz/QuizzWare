using Microsoft.Extensions.Logging;
using QuizzWare.Core.Models;
using QuizzWare.Core.Progress;

namespace QuizzWare.Services;

/// <summary>
/// Garde les rappels à jour : après chaque quizz ou changement de réglage, et quand l'app revient au premier plan
/// (l'objectif du jour, la série et les révisions dues ont pu changer).
/// </summary>
public sealed class ReminderCoordinator(ProgressService progressService, IReminderService reminders, ILogger<ReminderCoordinator> logger)
{
    private bool _started;

    public void Start()
    {
        if (_started)
            return;
        _started = true;
        progressService.Changed += async (_, progress) => await RescheduleAsync(progress);
    }

    public async Task RescheduleAsync(UserProgress? progress = null)
    {
        if (!reminders.IsSupported)
            return;

        try
        {
            progress ??= await progressService.GetProgressAsync();
            var plan = ReminderPlanner.Plan(
                progress.Settings,
                progressService.GetDailyGoal(progress),
                progressService.GetCurrentStreak(progress),
                progressService.CountDueReviews(progress),
                DateTime.Now);
            await reminders.ScheduleAsync(plan);
        }
        catch (Exception ex)
        {
            // Un rappel manqué ne doit jamais faire planter l'application.
            logger.LogWarning(ex, "Impossible de programmer les rappels");
        }
    }
}
