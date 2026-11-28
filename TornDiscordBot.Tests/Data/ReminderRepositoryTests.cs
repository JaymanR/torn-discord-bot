using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using TornDiscordBot.Data;
using TornDiscordBot.Entities;

namespace TornDiscordBot.Tests.Data;

/// <summary>Tests for <see cref="ReminderRepository"/> against a real, temporary SQLite database.</summary>
public sealed class ReminderRepositoryTests : IAsyncLifetime
{
    private const ulong User = 100_000_000_000_000_001;
    private const ulong OtherUser = 100_000_000_000_000_002;

    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"bot-tests-{Guid.NewGuid():N}.db");
    private ServiceProvider _services = null!;
    private IDbContextFactory<BotDbContext> _factory = null!;
    private ReminderRepository _repository = null!;

    public async Task InitializeAsync()
    {
        // Pooling is off so the file is released as soon as each context is disposed.
        _services = new ServiceCollection()
            .AddDbContextFactory<BotDbContext>(o => o.UseSqlite($"Data Source={_databasePath};Pooling=False"))
            .BuildServiceProvider();
        _factory = _services.GetRequiredService<IDbContextFactory<BotDbContext>>();

        await using var db = await _factory.CreateDbContextAsync();
        await db.Database.MigrateAsync();
        db.RegisteredUsers.AddRange(
            new RegisteredUser { DiscordUserId = User, EncryptedTornApiKey = [1] },
            new RegisteredUser { DiscordUserId = OtherUser, EncryptedTornApiKey = [1] });
        await db.SaveChangesAsync();

        _repository = new ReminderRepository(_factory);
    }

    public async Task DisposeAsync()
    {
        await _services.DisposeAsync();
        File.Delete(_databasePath);
    }

    [Fact]
    public async Task RegisteredUser_HasNoRemindersOn()
    {
        Assert.Empty(await _repository.GetEnabledTypesAsync(User));
        Assert.Empty(await _repository.GetDueUserIdsAsync(ReminderType.DrugCooldown, Now));
    }

    [Fact]
    public async Task SetEnabledTypesAsync_ChangesOnlyThatUser()
    {
        await _repository.SetEnabledTypesAsync(User, [ReminderType.DrugCooldown]);
        await _repository.SetEnabledTypesAsync(OtherUser, [ReminderType.DrugCooldown]);

        await _repository.SetEnabledTypesAsync(User, []);

        Assert.Empty(await _repository.GetEnabledTypesAsync(User));
        Assert.Equal([ReminderType.DrugCooldown], await _repository.GetEnabledTypesAsync(OtherUser));
        Assert.Equal([OtherUser], await _repository.GetDueUserIdsAsync(ReminderType.DrugCooldown, Now));
    }

    [Fact]
    public async Task UpdateAsync_DoesNotTurnAReminderBackOn()
    {
        await _repository.SetEnabledTypesAsync(User, [ReminderType.DrugCooldown]);
        var reminder = await _repository.GetAsync(User, ReminderType.DrugCooldown);
        await _repository.SetEnabledTypesAsync(User, []);

        await _repository.UpdateAsync(reminder!);

        Assert.Empty(await _repository.GetEnabledTypesAsync(User));
    }

    [Fact]
    public async Task Migrating_RemovesRemindersCreatedAutomatically()
    {
        await using var db = await _factory.CreateDbContextAsync();
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync("AddReminders");
        db.Reminders.Add(new Reminder { DiscordUserId = User, Type = ReminderType.DrugCooldown });
        await db.SaveChangesAsync();

        await migrator.MigrateAsync();

        Assert.Empty(await _repository.GetEnabledTypesAsync(User));
    }
}
