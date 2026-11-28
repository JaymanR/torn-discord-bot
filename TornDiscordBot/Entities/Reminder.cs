namespace TornDiscordBot.Entities;

/// <summary>A kind of reminder that a registered user has turned on, with its progress.</summary>
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
