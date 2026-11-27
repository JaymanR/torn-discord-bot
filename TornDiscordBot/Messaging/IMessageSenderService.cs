namespace TornDiscordBot.Messaging;

/// <summary>Sends messages to Discord.</summary>
public interface IMessageSenderService
{
    /// <summary>Sends a text message to a channel.</summary>
    /// <param name="channelId">The ID of the target channel.</param>
    /// <param name="message">The message content.</param>
    public Task SendAsync(ulong channelId, string message);
}