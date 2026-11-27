using System.Net;
using System.Security.Cryptography;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TornDiscordBot.Extensions.Startup;
using TornDiscordBot.Interfaces;
using TornDiscordBot.Models;

namespace TornDiscordBot.Tests;

/// <summary>
/// Integration tests for the database, registration and Torn setup used at startup.
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
    public void AddRegistration_Throws_WhenMasterKeyIsMissing()
    {
        var builder = Host.CreateEmptyApplicationBuilder(settings: null);

        Assert.Throws<InvalidOperationException>(() => builder.AddRegistration());
    }

    /// <summary>
    /// Builds a host with the same setup calls as <c>Program.cs</c>, apart from Discord and messaging.
    /// Building fails if a registered service has a missing dependency.
    /// </summary>
    private IHost BuildHost()
    {
        // An empty builder ignores this machine's environment variables, settings files and user secrets.
        var builder = Host.CreateEmptyApplicationBuilder(settings: null);
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Database:Path"] = DatabasePath,
            ["Security:MasterKey"] = _masterKey
        });
        builder.ConfigureContainer(new DefaultServiceProviderFactory(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true }));

        builder.AddDatabase();
        builder.AddRegistration();
        builder.AddTornClient();

        // Applies to every HTTP client, so no request can reach the network.
        builder.Services.ConfigureHttpClientDefaults(http => http.ConfigurePrimaryHttpMessageHandler(() => _torn));

        return builder.Build();
    }

    /// <summary>Answers every request with empty cooldowns and records the address requested.</summary>
    private sealed class FakeTornHandler : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            RequestUri = request.RequestUri;

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"cooldowns":{"drug":0,"medical":0,"booster":0}}""")
            });
        }
    }
}
