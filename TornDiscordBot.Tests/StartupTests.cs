using System.Net;
using System.Security.Cryptography;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using TornDiscordBot.Extensions.Startup;
using TornDiscordBot.Interfaces;
using TornDiscordBot.Models;
using TornDiscordBot.Models.Reminders;

namespace TornDiscordBot.Tests;

/// <summary>
/// Integration tests for the database, registration, Torn and reminder setup used at startup.
/// Discord is not involved, and Torn is never called.
/// </summary>
public sealed class StartupTests : IDisposable
{
    private const string Key = "AbCdEfGh12345678";
    private const ulong UserId = 42;

    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"bot-startup-tests-{Guid.NewGuid():N}");
    private readonly string _masterKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    private readonly FakeTornHandler _torn = new();

    /// <summary>A database path whose directory doesn't exist yet.</summary>
    private string DatabasePath => Path.Combine(_directory, "data", "bot.db");

    public void Dispose()
    {
        // Pooled connections keep the file open until the pool is cleared.
        using (var connection = new SqliteConnection($"Data Source={DatabasePath}"))
            SqliteConnection.ClearPool(connection);

        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    [Fact]
    public async Task MigrateDatabaseAsync_CreatesDatabaseAtConfiguredPath()
    {
        using var host = BuildHost();

        await host.MigrateDatabaseAsync();

        Assert.True(File.Exists(DatabasePath));
    }

    [Fact]
    public async Task SavedKey_SurvivesRestart()
    {
        using (var host = BuildHost())
        {
            await host.MigrateDatabaseAsync();
            await host.Services.GetRequiredService<IRegisteredUserRepository>().SaveTornApiKeyAsync(UserId, Key);
        }

        using var restarted = BuildHost();
        await restarted.MigrateDatabaseAsync();
        var users = restarted.Services.GetRequiredService<IRegisteredUserRepository>();

        Assert.Equal(Key, await users.GetTornApiKeyAsync(UserId));
    }

    [Fact]
    public async Task TornClient_RequestsCooldownsFromTornApi()
    {
        using var host = BuildHost();

        var result = await host.Services.GetRequiredService<ITornClient>().ValidateKeyAsync(Key);

        Assert.Equal(KeyValidationResult.Valid, result);
        Assert.Equal("https://api.torn.com/v2/user/cooldowns", _torn.RequestUri?.ToString());
    }

    [Fact]
    public async Task StartedHost_ChecksRegisteredUsersCooldowns()
    {
        using var host = BuildHost();
        await host.MigrateDatabaseAsync();
        await host.Services.GetRequiredService<IRegisteredUserRepository>().SaveTornApiKeyAsync(UserId, Key);

        await host.StartAsync();
        await _torn.FirstRequest.WaitAsync(TimeSpan.FromSeconds(10));
        await host.StopAsync();
    }

    [Fact]
    public void AddRegistration_Throws_WhenMasterKeyIsMissing()
    {
        var builder = Host.CreateEmptyApplicationBuilder(settings: null);

        Assert.Throws<InvalidOperationException>(() => builder.AddRegistration());
    }

    [Fact]
    public void AddReminders_ReadsChannelFromConfiguration()
    {
        using var host = BuildHost(channelId: "123456789012345678");

        var options = host.Services.GetRequiredService<IOptions<ReminderOptions>>().Value;

        Assert.Equal(123456789012345678UL, options.ChannelId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("0")]
    [InlineData("general")]
    public async Task StartedHost_Throws_WhenChannelIsMissingOrInvalid(string? channelId)
    {
        using var host = BuildHost(channelId);

        var error = await Assert.ThrowsAnyAsync<Exception>(() => host.StartAsync());

        Assert.Contains("Reminders:ChannelId", error.Message);
    }

    /// <summary>
    /// Builds a host with the same setup calls as <c>Program.cs</c>, apart from Discord and messaging.
    /// Building fails if a registered service has a missing dependency.
    /// </summary>
    private IHost BuildHost(string? channelId = "1")
    {
        // An empty builder ignores this machine's environment variables, settings files and user secrets.
        var builder = Host.CreateEmptyApplicationBuilder(settings: null);
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Database:Path"] = DatabasePath,
            ["Security:MasterKey"] = _masterKey,
            ["Reminders:ChannelId"] = channelId
        });
        builder.ConfigureContainer(new DefaultServiceProviderFactory(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true }));

        builder.AddDatabase();
        builder.AddRegistration();
        builder.AddTornClient();
        builder.AddReminderService();
        builder.Services.AddSingleton<IMessageSenderService, SilentSender>(); // stands in for Discord

        // Applies to every HTTP client, so no request can reach the network.
        builder.Services.ConfigureHttpClientDefaults(http => http.ConfigurePrimaryHttpMessageHandler(() => _torn));

        return builder.Build();
    }

    /// <summary>Discards messages.</summary>
    private sealed class SilentSender : IMessageSenderService
    {
        public Task SendAsync(ulong channelId, string message) => Task.CompletedTask;
    }

    /// <summary>Answers every request with empty cooldowns and records the address requested.</summary>
    private sealed class FakeTornHandler : HttpMessageHandler
    {
        private readonly TaskCompletionSource _firstRequest = new();

        public Uri? RequestUri { get; private set; }

        /// <summary>Completes when the first request arrives.</summary>
        public Task FirstRequest => _firstRequest.Task;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            RequestUri = request.RequestUri;
            _firstRequest.TrySetResult();

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"cooldowns":{"drug":0,"medical":0,"booster":0}}""")
            });
        }
    }
}
