namespace TornDiscordBot.Entities;

/// <summary>The progress of one kind of reminder for a registered user.</summary>
public class Reminder
{
    public ulong DiscordUserId { get; set; }
    public ReminderType Type { get; set; }

    /// <summary>How many messages have been sent since the bot last saw the cooldown running.</summary>
    public int SentCount { get; set; }

    public DateTimeOffset? LastSentAt { get; set; }

    /// <summary>When Torn should next be asked about this reminder.</summary>
    public DateTimeOffset NextCheckAt { get; set; }
}
