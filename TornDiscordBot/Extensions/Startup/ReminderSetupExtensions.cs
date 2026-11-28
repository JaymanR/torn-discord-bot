using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using TornDiscordBot.Data;
using TornDiscordBot.Interfaces.Reminders;
using TornDiscordBot.Models.Reminders;
using TornDiscordBot.Services.Reminders;

namespace TornDiscordBot.Extensions.Startup;

/// <summary>Setup for the reminder feature.</summary>
public static class ReminderSetupExtensions
{
    extension(IHostApplicationBuilder builder)
    {
        /// <summary>
        /// Registers the reminder background service, which posts to the channel set in <c>Reminders:ChannelId</c>.
        /// The host fails to start if the channel ID is missing or invalid.
        /// </summary>
        public void AddReminderService()
        {
            builder.Services.AddOptions<ReminderOptions>()
                .BindConfiguration("Reminders")
                .Validate(options => options.ChannelId != 0,
                    "Reminder channel is not configured. Set Reminders:ChannelId to a Discord channel ID.")
                .ValidateOnStart();

            builder.Services.TryAddSingleton(TimeProvider.System);
            builder.Services.AddSingleton<IReminderRepository, ReminderRepository>();
            builder.Services.AddTransient<IReminderService, ReminderService>();
            builder.Services.AddHostedService<ReminderBackgroundService>();
        }
    }
}
