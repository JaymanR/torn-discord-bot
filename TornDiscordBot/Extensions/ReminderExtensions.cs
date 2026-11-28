using TornDiscordBot.Entities;
using TornDiscordBot.Models;

namespace TornDiscordBot.Extensions;

/// <summary>
/// Adds the rules to <see cref="Reminder"/> for when a message is sent and when Torn is checked again.
/// </summary>
public static class ReminderExtensions
{
    /// <summary>How often Torn is checked when there's no cooldown to wait for, or it couldn't be read.</summary>
    public static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(5);

    /// <summary>How long to wait before trying a key that Torn rejected again.</summary>
    public static readonly TimeSpan RejectedKeyInterval = TimeSpan.FromHours(1);

    /// <summary>The wait before each message, counted from the previous one. The first has no wait.</summary>
    private static readonly TimeSpan[] Delays = [TimeSpan.Zero, TimeSpan.FromMinutes(15), TimeSpan.FromHours(8)];

    extension(Reminder)
    {
        /// <summary>
        /// Creates a reminder that has just been turned on. It sends nothing until a cooldown has been seen, so
        /// turning it on with no cooldown running doesn't trigger messages.
        /// </summary>
        /// <param name="discordUserId">The Discord user being reminded.</param>
        /// <param name="type">The kind of reminder.</param>
        public static Reminder Create(ulong discordUserId, ReminderType type) =>
            new() { DiscordUserId = discordUserId, Type = type, SentCount = Delays.Length };
    }

    /// <param name="reminder">The reminder being read or changed.</param>
    extension(Reminder reminder)
    {
        /// <summary>Applies the latest cooldown and sets the next check time.</summary>
        /// <param name="remaining">The time left on the cooldown.</param>
        /// <param name="now">The current time.</param>
        public void Update(TimeSpan remaining, DateTimeOffset now)
        {
            if (remaining > TimeSpan.Zero)
                reminder.Reset(cooldownEndsAt: now + remaining);
            else
                reminder.NextCheckAt = reminder.CalculateNextCheckTime(now);
        }

        /// <summary>Checks whether a message should be sent now. None is due while a cooldown is running.</summary>
        /// <param name="remaining">The time left on the cooldown.</param>
        /// <param name="now">The current time.</param>
        /// <returns><see langword="true"/> if a message should be sent now.</returns>
        public bool IsMessageDue(TimeSpan remaining, DateTimeOffset now) =>
            remaining <= TimeSpan.Zero && reminder.DueAt() <= now;

        /// <summary>Records that a message was sent.</summary>
        /// <param name="now">The time the message was sent.</param>
        public void MarkSent(DateTimeOffset now)
        {
            reminder.SentCount++;
            reminder.LastSentAt = now;
        }

        /// <summary>
        /// Sets when to try again after Torn didn't return the cooldowns. The sent count and last sent time are kept.
        /// </summary>
        /// <param name="keyStatus">What the failed request showed about the key.</param>
        /// <param name="now">The current time.</param>
        public void Postpone(KeyValidationResult keyStatus, DateTimeOffset now)
        {
            var rejected = keyStatus is KeyValidationResult.Invalid or KeyValidationResult.AccessLevelTooLow;
            reminder.NextCheckAt = now + (rejected ? RejectedKeyInterval : CheckInterval);
        }

        /// <summary>
        /// Resets the sent count and last sent time, and sets the next check to when the cooldown ends.
        /// </summary>
        private void Reset(DateTimeOffset cooldownEndsAt)
        {
            reminder.SentCount = 0;
            reminder.LastSentAt = null;
            reminder.NextCheckAt = cooldownEndsAt;
        }

        /// <summary>
        /// When the next message is due, ignoring any running cooldown, or <see langword="null"/> if all have been
        /// sent.
        /// </summary>
        private DateTimeOffset? DueAt()
        {
            if (reminder.SentCount >= Delays.Length) return null;

            return reminder.LastSentAt is { } lastSentAt
                ? lastSentAt + Delays[reminder.SentCount]
                : DateTimeOffset.MinValue;
        }

        /// <summary>
        /// A routine check, or the next message's due time if that comes sooner. Routine checks continue between
        /// messages to notice when the user starts a new cooldown.
        /// </summary>
        private DateTimeOffset CalculateNextCheckTime(DateTimeOffset now)
        {
            var routine = now + CheckInterval;

            return reminder.DueAt() is { } due && due > now && due < routine ? due : routine;
        }
    }
}
