using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TornDiscordBot.Interfaces.Reminders;

namespace TornDiscordBot.Services.Reminders;

/// <summary>Runs the <see cref="IReminderService"/> in the background for as long as the bot is running.</summary>
public sealed class ReminderBackgroundService(
    IServiceScopeFactory scopes,
    TimeProvider time,
    ILogger<ReminderBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromMinutes(1);

    /// <summary>Processes due reminders at startup, then once a minute.</summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TickInterval, time);

        do
        {
            try
            {
                using var scope = scopes.CreateScope();
                await scope.ServiceProvider.GetRequiredService<IReminderService>().ProcessDueAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Checking reminders failed");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
