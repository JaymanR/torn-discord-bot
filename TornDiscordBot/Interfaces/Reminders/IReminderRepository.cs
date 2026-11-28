using TornDiscordBot.Entities;

namespace TornDiscordBot.Interfaces.Reminders;

/// <summary>Persists the reminders users have turned on, with their progress.</summary>
public interface IReminderRepository
{
    /// <summary>Gets the users who have a kind of reminder turned on and should be checked.</summary>
    /// <param name="type">The kind of reminder.</param>
    /// <param name="now">The current time.</param>
    /// <returns>Users whose next check time has arrived.</returns>
    Task<IReadOnlyList<ulong>> GetDueUserIdsAsync(ReminderType type, DateTimeOffset now);

    /// <summary>Gets a user's progress for a kind of reminder.</summary>
    /// <param name="discordUserId">The Discord user being reminded.</param>
    /// <param name="type">The kind of reminder.</param>
    /// <returns>The reminder, or <see langword="null"/> if the user doesn't have it turned on.</returns>
    Task<Entities.Reminder?> GetAsync(ulong discordUserId, ReminderType type);

    /// <summary>Gets the kinds of reminder a user has turned on.</summary>
    /// <param name="discordUserId">The Discord user being reminded.</param>
    Task<IReadOnlyList<ReminderType>> GetEnabledTypesAsync(ulong discordUserId);

    /// <summary>
    /// Turns on the given kinds of reminder for a user and turns off all others. Reminders that were already on
    /// keep their progress.
    /// </summary>
    /// <param name="discordUserId">The Discord user being reminded. Must be registered.</param>
    /// <param name="types">The kinds of reminder to have on.</param>
    Task SetEnabledTypesAsync(ulong discordUserId, IReadOnlyCollection<ReminderType> types);

    /// <summary>Saves a reminder's progress. Does nothing if the reminder has been turned off.</summary>
    /// <param name="reminder">The reminder to save.</param>
    Task UpdateAsync(Entities.Reminder reminder);
}
