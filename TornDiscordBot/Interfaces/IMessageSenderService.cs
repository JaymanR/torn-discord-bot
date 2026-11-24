namespace TornDiscordBot.Interfaces;

public interface IMessageSenderService
{
    public Task SendAsync(ulong channelId, string message);
}