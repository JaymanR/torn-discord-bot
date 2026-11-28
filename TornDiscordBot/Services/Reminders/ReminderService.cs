using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TornDiscordBot.Entities;
using TornDiscordBot.Extensions;
using TornDiscordBot.Interfaces;
using TornDiscordBot.Interfaces.Reminders;
using TornDiscordBot.Models.Reminders;

namespace TornDiscordBot.Services.Reminders;

/// <summary>Checks the users who are due and sends their reminders.</summary>
public sealed class ReminderService(
    IReminderRepository reminders,
    IRegisteredUserRepository users,
    ITornClient torn,
    IMessageSenderService messages,
    IOptions<ReminderOptions> options,
    TimeProvider time,
    ILogger<ReminderService> logger) : IReminderService
{
    /// <inheritdoc />
    public async Task ProcessDueAsync()
    {
        var dueUserIds = await reminders.GetDueUserIdsAsync(ReminderType.DrugCooldown, time.GetUtcNow());

        foreach (var userId in dueUserIds)
        {
            try
            {
                await ProcessDrugCooldownAsync(userId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Drug cooldown reminder failed for Discord user {UserId}", userId);
            }
        }
    }

    /// <summary>Asks Torn for the user's drug cooldown, sends a reminder if one is due, and saves the progress.</summary>
    private async Task ProcessDrugCooldownAsync(ulong userId)
    {
        var apiKey = await users.GetTornApiKeyAsync(userId);
        if (apiKey is null) return;

        var result = await torn.GetCooldownsAsync(apiKey);
        var reminder = await reminders.GetAsync(userId, ReminderType.DrugCooldown)
                       ?? Reminder.Create(userId, ReminderType.DrugCooldown);
        var now = time.GetUtcNow();

        if (result.Cooldowns is not { } cooldowns)
        {
            reminder.Postpone(result.KeyStatus, now);
        }
        else
        {
            reminder.Update(cooldowns.Drug, now);

            if (reminder.IsMessageDue(cooldowns.Drug, now))
            {
                await messages.SendAsync(options.Value.ChannelId, $"<@{userId}> your drug cooldown is over. Go to https://www.torn.com/item.php to use one.");
                reminder.MarkSent(now);
            }
        }

        await reminders.SaveAsync(reminder);
    }
}
