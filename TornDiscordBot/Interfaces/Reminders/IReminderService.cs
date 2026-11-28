namespace TornDiscordBot.Interfaces.Reminders;

/// <summary>Checks the users who are due and sends their reminders.</summary>
public interface IReminderService
{
    /// <summary>
    /// Processes every user who is due. A failure for one user is logged and doesn't stop the others; that user is
    /// tried again on the next run.
    /// </summary>
    Task ProcessDueAsync();
}
