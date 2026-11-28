using TornDiscordBot.Entities;

namespace TornDiscordBot.Interfaces.Reminders;

/// <summary>Persists reminder progress.</summary>
public interface IReminderRepository
{
    /// <summary>Gets the registered users who should be checked for a kind of reminder.</summary>
    /// <param name="type">The kind of reminder.</param>
    /// <param name="now">The current time.</param>
    /// <returns>Users whose next check time has arrived, including those never checked.</returns>
    Task<IReadOnlyList<ulong>> GetDueUserIdsAsync(ReminderType type, DateTimeOffset now);

    /// <summary>Gets a user's progress for a kind of reminder.</summary>
    /// <param name="discordUserId">The Discord user being reminded.</param>
    /// <param name="type">The kind of reminder.</param>
    /// <returns>The reminder, or <see langword="null"/> if the user has never been checked.</returns>
    Task<Entities.Reminder?> GetAsync(ulong discordUserId, ReminderType type);

    /// <summary>Inserts or updates a reminder.</summary>
    /// <param name="reminder">The reminder to save. Its user must be registered.</param>
    Task SaveAsync(Entities.Reminder reminder);
}
