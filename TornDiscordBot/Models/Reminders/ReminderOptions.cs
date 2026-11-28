namespace TornDiscordBot.Models.Reminders;

/// <summary>Reminder settings, read from the <c>Reminders</c> configuration section.</summary>
public sealed class ReminderOptions
{
    /// <summary>The Discord channel where reminders are posted.</summary>
    public ulong ChannelId { get; set; }
}
