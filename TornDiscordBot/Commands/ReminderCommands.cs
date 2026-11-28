using Microsoft.Extensions.Options;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using TornDiscordBot.Builders;
using TornDiscordBot.Interfaces;
using TornDiscordBot.Interfaces.Reminders;
using TornDiscordBot.Models.Reminders;

namespace TornDiscordBot.Commands;

/// <summary>Slash command for choosing which reminders to receive.</summary>
public class ReminderCommands(
    IRegisteredUserRepository users,
    IReminderRepository reminders,
    IOptions<ReminderOptions> options) : ApplicationCommandModule<ApplicationCommandContext>
{
    /// <summary>Privately shows the user a menu of reminders, with the ones they have on already selected.</summary>
    [SlashCommand("reminders", "Choose which reminders you get")]
    public async Task RemindersAsync()
    {
        var reply = new InteractionMessageProperties { Flags = MessageFlags.Ephemeral };

        if (await users.IsRegisteredAsync(Context.User.Id))
        {
            var enabledReminderTypes = await reminders.GetEnabledTypesAsync(Context.User.Id);
            reply.Content = ReminderMenuBuilder.Summarize(enabledReminderTypes, options.Value.ChannelId);
            reply.Components = [ReminderMenuBuilder.CreateMenu(enabledReminderTypes)];
        }
        else
        {
            reply.Content = ReminderMenuBuilder.NotRegisteredReply;
        }

        await RespondAsync(InteractionCallback.Message(reply));
    }
}
