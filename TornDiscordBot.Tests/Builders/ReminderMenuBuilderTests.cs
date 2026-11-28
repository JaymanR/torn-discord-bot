using TornDiscordBot.Builders;
using TornDiscordBot.Entities;

namespace TornDiscordBot.Tests.Builders;

/// <summary>Tests for the reminder menu and the message shown with it.</summary>
public sealed class ReminderMenuBuilderTests
{
    [Fact]
    public void CreateMenu_OffersEveryReminderType()
    {
        var menu = ReminderMenuBuilder.CreateMenu([]);

        Assert.Equal(
            Enum.GetValues<ReminderType>().Select(type => type.ToString()),
            menu.Options.Select(option => option.Value));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CreateMenu_SelectsOnlyRemindersThatAreOn(bool on)
    {
        var menu = ReminderMenuBuilder.CreateMenu(on ? [ReminderType.DrugCooldown] : []);

        Assert.Equal(on, Assert.Single(menu.Options).Default);
    }

    [Fact]
    public void CreateMenu_AllowsSelectingNothing()
    {
        Assert.Equal(0, ReminderMenuBuilder.CreateMenu([]).MinValues);
    }

    [Fact]
    public void Summarize_NamesTheRemindersOnAndTheChannel()
    {
        var summary = ReminderMenuBuilder.Summarize([ReminderType.DrugCooldown], channelId: 1000);

        Assert.Contains("Drug cooldown", summary);
        Assert.Contains("<#1000>", summary);
    }

    [Fact]
    public void Summarize_SaysWhenNoRemindersAreOn()
    {
        Assert.StartsWith("You have no reminders on.", ReminderMenuBuilder.Summarize([], channelId: 1000));
    }
}
