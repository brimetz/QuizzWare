using QuizzWare.Core.Progress;

namespace QuizzWare.Services;

/// <summary>Programme les rappels quotidiens avec le système de notifications de la plateforme.</summary>
public interface IReminderService
{
    /// <summary>Faux sur les plateformes sans notifications locales (Windows, utilisé pour le développement).</summary>
    bool IsSupported { get; }

    /// <summary>Demande l'autorisation d'afficher des notifications ; renvoie vrai si elle est accordée.</summary>
    Task<bool> RequestPermissionAsync();

    /// <summary>Remplace les rappels programmés par ceux du plan.</summary>
    Task ScheduleAsync(IReadOnlyList<PlannedReminder> reminders);

    /// <summary>Programme un rappel isolé (rappel de test), sans toucher aux rappels quotidiens.</summary>
    Task ScheduleOneAsync(PlannedReminder reminder);
}

/// <summary>Plateformes sans notifications locales : rien n'est programmé.</summary>
public sealed class NoReminderService : IReminderService
{
    public bool IsSupported => false;
    public Task<bool> RequestPermissionAsync() => Task.FromResult(false);
    public Task ScheduleAsync(IReadOnlyList<PlannedReminder> reminders) => Task.CompletedTask;
    public Task ScheduleOneAsync(PlannedReminder reminder) => Task.CompletedTask;
}
