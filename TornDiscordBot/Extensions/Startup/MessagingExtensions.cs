using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TornDiscordBot.Interfaces;
using TornDiscordBot.Services;

namespace TornDiscordBot.Extensions.Startup;

/// <summary>Setup for outgoing Discord messages.</summary>
public static class MessagingExtensions
{
    extension(IHostApplicationBuilder builder)
    {
        /// <summary>Registers the <see cref="IMessageSenderService"/>.</summary>
        public void AddMessagingService()
        {
            builder.Services.AddSingleton<IMessageSenderService, MessageSenderService>();
        }
    }
}
