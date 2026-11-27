using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace TornDiscordBot.Messaging;

/// <summary>Setup for outgoing Discord messages.</summary>
public static class MessagingExtensions
{
    extension(IHostApplicationBuilder builder)
    {
        /// <summary>Registers the <see cref="IMessageSenderService"/>.</summary>
        public void AddMessaging()
        {
            builder.Services.AddSingleton<IMessageSenderService, MessageSenderService>();
        }
    }
}
