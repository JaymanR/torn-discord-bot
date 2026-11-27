using NetCord.Rest;

namespace TornDiscordBot.Messaging;

/// <summary>Sends messages through the Discord REST API.</summary>
public class MessageSenderService(RestClient client) : IMessageSenderService
{
    /// <inheritdoc />
    public Task SendAsync(ulong channelId, string message)
    {
        return client.SendMessageAsync(channelId, new MessageProperties { Content = message });
    }
}