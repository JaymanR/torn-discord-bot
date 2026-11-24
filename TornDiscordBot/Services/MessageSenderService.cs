using NetCord.Rest;
using TornDiscordBot.Interfaces;

namespace TornDiscordBot.Services;

public class MessageSenderService(RestClient client) : IMessageSenderService
{
    public Task SendAsync(ulong channelId, string message)
    {
        return client.SendMessageAsync(channelId, new MessageProperties { Content = message });
    }
}