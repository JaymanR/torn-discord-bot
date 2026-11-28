using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TornDiscordBot.Data;
using TornDiscordBot.Entities;
using TornDiscordBot.Interfaces;
using TornDiscordBot.Models;
using TornDiscordBot.Models.Reminders;
using TornDiscordBot.Models.Torn;
using TornDiscordBot.Security;
using TornDiscordBot.Services.Reminders;

namespace TornDiscordBot.Tests.Services;

/// <summary>
/// Integration tests for drug cooldown reminders, using a real temporary SQLite database, a controllable clock,
/// and stand-ins for Torn and Discord.
/// </summary>
public sealed class ReminderServiceTests : IAsyncLifetime
{
    private const string Key = "AbCdEfGh12345678";
    private const ulong UserId = 42;
    private const ulong ChannelId = 1000;

    private static readonly DateTimeOffset Start = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"bot-reminder-tests-{Guid.NewGuid():N}.db");
    private readonly TestClock _clock = new() { Now = Start };
    private readonly FakeTornClient _torn;
    private readonly FakeSender _sender;
    private ServiceProvider _services = null!;
    private RegisteredUserRepository _users = null!;
    private ReminderRepository _reminders = null!;
    private ReminderService _reminderService = null!;

    public ReminderServiceTests()
    {
        _torn = new FakeTornClient(_clock);
        _sender = new FakeSender(_clock);
    }

    public async Task InitializeAsync()
    {
        _services = new ServiceCollection()
            .AddDbContextFactory<BotDbContext>(o => o.UseSqlite($"Data Source={_databasePath};Pooling=False"))
            .BuildServiceProvider();
        var factory = _services.GetRequiredService<IDbContextFactory<BotDbContext>>();

        await using var db = await factory.CreateDbContextAsync();
        await db.Database.MigrateAsync();

        _users = new RegisteredUserRepository(factory, new SecretProtector(RandomNumberGenerator.GetBytes(32)));
        _reminders = new ReminderRepository(factory);
        var options = Options.Create(new ReminderOptions { ChannelId = ChannelId });
        _reminderService = new ReminderService(_reminders, _users, _torn, _sender, options, _clock,
            NullLogger<ReminderService>.Instance);

        await _users.SaveTornApiKeyAsync(UserId, Key);
    }

    public async Task DisposeAsync()
    {
        await _services.DisposeAsync();
        File.Delete(_databasePath);
    }

    [Fact]
    public async Task SendsWhenCooldownEnds_ThenAfter15Minutes_ThenAfter8Hours_ThenStops()
    {
        _torn.DrugCooldownEndsAt = Start + Hours(1);

        await RunUntilAsync(Hours(24));

        Assert.Equal([Hours(1), Hours(1, 15), Hours(9, 15)], _sender.SentAfter);
        var expected = $"<@{UserId}> your drug cooldown is over. Go to https://www.torn.com/item.php to use one.";
        Assert.All(_sender.Messages, message => Assert.Equal((ChannelId, expected), message));
    }

    [Fact]
    public async Task SendsNothing_WhenNoCooldownHasBeenSeen()
    {
        await RunUntilAsync(Hours(24));

        Assert.Empty(_sender.Messages);
    }

    [Fact]
    public async Task NewCooldown_RestartsReminders()
    {
        _torn.DrugCooldownEndsAt = Start + Hours(1);
        await RunUntilAsync(Hours(1, 4));

        _torn.DrugCooldownEndsAt = Start + Hours(7); // the user takes another drug after the first message
        await RunUntilAsync(Hours(24));

        Assert.Equal([Hours(1), Hours(7), Hours(7, 15), Hours(15, 15)], _sender.SentAfter);
    }

    [Fact]
    public async Task DoesNotAskTornAgain_UntilCooldownEnds()
    {
        _torn.DrugCooldownEndsAt = Start + Hours(6);

        await RunUntilAsync(Hours(5, 59));

        Assert.Equal(1, _torn.Requests);
    }

    [Fact]
    public async Task FailedMessage_IsRetried()
    {
        _torn.DrugCooldownEndsAt = Start + Hours(1);
        _sender.FailFor = UserId;
        await RunUntilAsync(Hours(1, 2));

        _sender.FailFor = null;
        await RunUntilAsync(Hours(24));

        Assert.Equal([Hours(1, 3), Hours(1, 18), Hours(9, 18)], _sender.SentAfter);
    }

    [Fact]
    public async Task OneUsersFailure_DoesNotStopOthers()
    {
        const ulong otherUserId = 43;
        await _users.SaveTornApiKeyAsync(otherUserId, Key);
        _torn.DrugCooldownEndsAt = Start + Hours(1);
        _sender.FailFor = UserId;

        await RunUntilAsync(Hours(1));

        var (_, message) = Assert.Single(_sender.Messages);
        Assert.StartsWith($"<@{otherUserId}>", message);
    }

    [Fact]
    public async Task TornUnavailable_DelaysRemindersWithoutLosingProgress()
    {
        _torn.DrugCooldownEndsAt = Start + Hours(1);
        await RunUntilAsync(Hours(1, 4));

        _torn.Failure = KeyValidationResult.Unavailable;
        await RunUntilAsync(Hours(2));

        _torn.Failure = null;
        await RunUntilAsync(Hours(24));

        Assert.Equal([Hours(1), Hours(2, 5), Hours(10, 5)], _sender.SentAfter);
    }

    [Theory]
    [InlineData(KeyValidationResult.Invalid)]
    [InlineData(KeyValidationResult.AccessLevelTooLow)]
    public async Task RejectedKey_IsTriedOnlyOnceAnHour(KeyValidationResult rejection)
    {
        _torn.Failure = rejection;

        await RunUntilAsync(Hours(3));

        Assert.Equal(4, _torn.Requests); // at 0, 1, 2 and 3 hours
        Assert.Empty(_sender.Messages);
    }

    [Fact]
    public async Task DeletingUser_RemovesTheirReminder()
    {
        await RunUntilAsync(Hours(0));
        Assert.NotNull(await _reminders.GetAsync(UserId, ReminderType.DrugCooldown));

        await _users.DeleteAsync(UserId);

        Assert.Null(await _reminders.GetAsync(UserId, ReminderType.DrugCooldown));
    }

    private static TimeSpan Hours(int hours, int minutes = 0) => new(hours, minutes, 0);

    /// <summary>Runs the service once a minute, as the background service does, up to and including the given time.</summary>
    private async Task RunUntilAsync(TimeSpan sinceStart)
    {
        for (; _clock.Now <= Start + sinceStart; _clock.Now += TimeSpan.FromMinutes(1))
            await _reminderService.ProcessDueAsync();
    }

    /// <summary>A clock the test moves by hand.</summary>
    private sealed class TestClock : TimeProvider
    {
        public DateTimeOffset Now { get; set; }

        public override DateTimeOffset GetUtcNow() => Now;
    }

    /// <summary>Reports a drug cooldown based on the test clock, or a chosen failure.</summary>
    private sealed class FakeTornClient(TestClock clock) : ITornClient
    {
        public DateTimeOffset DrugCooldownEndsAt { get; set; } = Start;
        public KeyValidationResult? Failure { get; set; }
        public int Requests { get; private set; }

        public async Task<KeyValidationResult> ValidateKeyAsync(string apiKey) =>
            (await GetCooldownsAsync(apiKey)).KeyStatus;

        public Task<CooldownsResult> GetCooldownsAsync(string apiKey)
        {
            Requests++;

            if (Failure is { } failure)
                return Task.FromResult(new CooldownsResult(failure));

            var remaining = DrugCooldownEndsAt > clock.Now ? DrugCooldownEndsAt - clock.Now : TimeSpan.Zero;
            return Task.FromResult(new CooldownsResult(KeyValidationResult.Valid, new Cooldowns(remaining)));
        }
    }

    /// <summary>Records messages and when they were sent, and can fail for one user.</summary>
    private sealed class FakeSender(TestClock clock) : IMessageSenderService
    {
        public ulong? FailFor { get; set; }
        public List<(ulong ChannelId, string Message)> Messages { get; } = [];
        public List<TimeSpan> SentAfter { get; } = [];

        public Task SendAsync(ulong channelId, string message)
        {
            if (FailFor is { } userId && message.StartsWith($"<@{userId}>"))
                return Task.FromException(new HttpRequestException("Discord is unavailable"));

            Messages.Add((channelId, message));
            SentAfter.Add(clock.Now - Start);
            return Task.CompletedTask;
        }
    }
}
