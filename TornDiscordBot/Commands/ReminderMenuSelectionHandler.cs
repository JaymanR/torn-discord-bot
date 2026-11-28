using Microsoft.Extensions.Options;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;
using TornDiscordBot.Builders;
using TornDiscordBot.Entities;
using TornDiscordBot.Interfaces;
using TornDiscordBot.Interfaces.Reminders;
using TornDiscordBot.Models.Reminders;

namespace TornDiscordBot.Commands;

/// <summary>Handles selections in the menu opened by <c>/reminders</c>.</summary>
public class ReminderMenuSelectionHandler(
    IRegisteredUserRepository users,
    IReminderRepository reminders,
    IOptions<ReminderOptions> options) : ComponentInteractionModule<StringMenuInteractionContext>
{
    /// <summary>Turns on the selected reminders, turns off the rest, and updates the message to confirm.</summary>
    [ComponentInteraction(ReminderMenuBuilder.MenuId)]
    public async Task SelectAsync()
    {
        if (!await users.IsRegisteredAsync(Context.User.Id))
        {
            await RespondAsync(InteractionCallback.ModifyMessage(message =>
            {
                message.Content = ReminderMenuBuilder.NotRegisteredReply;
                message.Components = [];
            }));
            return;
        }

        var selectedReminderTypes = Context.SelectedValues.Select(Enum.Parse<ReminderType>).ToList();
        await reminders.SetEnabledTypesAsync(Context.User.Id, selectedReminderTypes);

        await RespondAsync(InteractionCallback.ModifyMessage(message =>
        {
            message.Content = ReminderMenuBuilder.Summarize(selectedReminderTypes, options.Value.ChannelId);
            message.Components = [ReminderMenuBuilder.CreateMenu(selectedReminderTypes)];
        }));
    }
}
