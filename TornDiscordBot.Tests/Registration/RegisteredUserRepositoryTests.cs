using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TornDiscordBot.Data;
using TornDiscordBot.Registration;
using TornDiscordBot.Security;

namespace TornDiscordBot.Tests.Registration;

/// <summary>Tests for <see cref="RegisteredUserRepository"/> against a real, temporary SQLite database.</summary>
public sealed class RegisteredUserRepositoryTests : IAsyncLifetime
{
    private const string Key = "AbCdEfGh12345678";
    private const string OtherKey = "ZyXwVuTs87654321";
    private const ulong User = 100_000_000_000_000_001;
    private const ulong OtherUser = ulong.MaxValue - 1; // above long.MaxValue, exercises the id conversion

    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"bot-tests-{Guid.NewGuid():N}.db");
    private ServiceProvider _services = null!;
    private IDbContextFactory<BotDbContext> _factory = null!;
    private RegisteredUserRepository _repository = null!;

    public async Task InitializeAsync()
    {
        // Pooling is off so the file is released as soon as each context is disposed.
        _services = new ServiceCollection()
            .AddDbContextFactory<BotDbContext>(o => o.UseSqlite($"Data Source={_databasePath};Pooling=False"))
            .BuildServiceProvider();
        _factory = _services.GetRequiredService<IDbContextFactory<BotDbContext>>();

        await using var db = await _factory.CreateDbContextAsync();
        await db.Database.MigrateAsync();

        _repository = new RegisteredUserRepository(_factory, new SecretProtector(RandomNumberGenerator.GetBytes(32)));
    }

    public async Task DisposeAsync()
    {
        await _services.DisposeAsync();
        File.Delete(_databasePath);
    }

    [Fact]
    public async Task Migrations_MatchTheModel()
    {
        await using var db = await _factory.CreateDbContextAsync();

        Assert.False(db.Database.HasPendingModelChanges());
    }

    [Fact]
    public async Task GetApiKeyAsync_ReturnsNull_WhenNothingStored()
    {
        Assert.Null(await _repository.GetTornApiKeyAsync(User));
    }

    [Fact]
    public async Task GetApiKeyAsync_ReturnsSavedKey()
    {
        await _repository.SaveTornApiKeyAsync(User, Key);

        Assert.Equal(Key, await _repository.GetTornApiKeyAsync(User));
    }

    [Fact]
    public async Task SaveApiKeyAsync_ReplacesKeyAndKeepsCreatedAt()
    {
        await _repository.SaveTornApiKeyAsync(User, Key);
        var createdAt = await GetCreatedAtAsync(User);
        await Task.Delay(20);

        await _repository.SaveTornApiKeyAsync(User, OtherKey);

        Assert.Equal(OtherKey, await _repository.GetTornApiKeyAsync(User));
        Assert.Equal(createdAt, await GetCreatedAtAsync(User));
    }

    [Fact]
    public async Task SaveApiKeyAsync_KeepsUsersSeparate()
    {
        await _repository.SaveTornApiKeyAsync(User, Key);
        await _repository.SaveTornApiKeyAsync(OtherUser, OtherKey);

        Assert.Equal(Key, await _repository.GetTornApiKeyAsync(User));
        Assert.Equal(OtherKey, await _repository.GetTornApiKeyAsync(OtherUser));
    }

    [Fact]
    public async Task SaveApiKeyAsync_DoesNotWritePlaintextToDisk()
    {
        await _repository.SaveTornApiKeyAsync(User, Key);

        var contents = Encoding.Latin1.GetString(await File.ReadAllBytesAsync(_databasePath));

        Assert.DoesNotContain(Key, contents);
    }

    [Fact]
    public async Task DeleteAsync_RemovesOnlyThatUser()
    {
        await _repository.SaveTornApiKeyAsync(User, Key);
        await _repository.SaveTornApiKeyAsync(OtherUser, OtherKey);

        Assert.True(await _repository.DeleteAsync(User));

        Assert.Null(await _repository.GetTornApiKeyAsync(User));
        Assert.Equal(OtherKey, await _repository.GetTornApiKeyAsync(OtherUser));
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenNothingStored()
    {
        Assert.False(await _repository.DeleteAsync(User));
    }

    private async Task<DateTimeOffset> GetCreatedAtAsync(ulong userId)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return (await db.RegisteredUsers.SingleAsync(u => u.DiscordUserId == userId)).CreatedAt;
    }
}
