using NetCord.Rest;
using TornDiscordBot.Entities;

namespace TornDiscordBot.Builders;

/// <summary>Builds the menu for choosing reminders and the text shown with it.</summary>
public static class ReminderMenuBuilder
{
    /// <summary>The ID Discord sends back when the user makes a selection.</summary>
    public const string MenuId = "reminder-menu";

    public const string NotRegisteredReply = "Link your Torn API key with `/register` first, then run `/reminders`.";

    /// <summary>The reminders a user can pick from, in the order they are listed.</summary>
    private static readonly (ReminderType Type, string Label, string Description)[] Choices =
    [
        (ReminderType.DrugCooldown, "Drug cooldown", "Mentions you when your drug cooldown ends")
    ];

    /// <summary>Creates the menu for picking any number of reminders, including none.</summary>
    /// <param name="enabled">The reminders to show as selected.</param>
    public static StringMenuProperties CreateMenu(IReadOnlyCollection<ReminderType> enabled)
    {
        var menuOptions = Choices.Select(choice =>
            new StringMenuSelectOptionProperties(choice.Label, choice.Type.ToString())
            {
                Description = choice.Description,
                Default = enabled.Contains(choice.Type)
            });

        return new StringMenuProperties(MenuId, menuOptions)
        {
            Placeholder = "No reminders selected",
            MinValues = 0,
            MaxValues = Choices.Length
        };
    }

    /// <summary>Describes which reminders are on and where they are posted.</summary>
    /// <param name="enabled">The reminders that are on.</param>
    /// <param name="channelId">The channel where reminders are posted.</param>
    public static string Summarize(IReadOnlyCollection<ReminderType> enabled, ulong channelId)
    {
        var labels = Choices.Where(choice => enabled.Contains(choice.Type)).Select(choice => choice.Label).ToList();

        return labels.Count == 0
            ? $"You have no reminders on. Pick the ones you want below. They're posted in <#{channelId}>."
            : $"Reminders on: **{string.Join(", ", labels)}**. They're posted in <#{channelId}>. " +
              "Change your choice below at any time.";
    }
}
