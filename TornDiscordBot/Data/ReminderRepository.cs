using Microsoft.EntityFrameworkCore;
using TornDiscordBot.Entities;
using TornDiscordBot.Extensions;
using TornDiscordBot.Interfaces.Reminders;

namespace TornDiscordBot.Data;

/// <summary>An <see cref="IReminderRepository"/> backed by EF Core.</summary>
public sealed class ReminderRepository(IDbContextFactory<BotDbContext> dbFactory) : IReminderRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<ulong>> GetDueUserIdsAsync(ReminderType type, DateTimeOffset now)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        return await db.Reminders
            .Where(r => r.Type == type && r.NextCheckAt <= now)
            .Select(r => r.DiscordUserId)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<Reminder?> GetAsync(ulong discordUserId, ReminderType type)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        return await db.Reminders
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.DiscordUserId == discordUserId && r.Type == type);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ReminderType>> GetEnabledTypesAsync(ulong discordUserId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        return await db.Reminders
            .Where(r => r.DiscordUserId == discordUserId)
            .Select(r => r.Type)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task SetEnabledTypesAsync(ulong discordUserId, IReadOnlyCollection<ReminderType> types)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        var enabled = await db.Reminders
            .Where(r => r.DiscordUserId == discordUserId)
            .ToListAsync();

        db.Reminders.RemoveRange(enabled.Where(r => !types.Contains(r.Type)));
        db.Reminders.AddRange(types
            .Except(enabled.Select(r => r.Type))
            .Select(type => Reminder.Create(discordUserId, type)));

        await db.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Reminder reminder)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        var stored = await db.Reminders.FindAsync([reminder.DiscordUserId, reminder.Type]);
        if (stored is null) return;

        db.Entry(stored).CurrentValues.SetValues(reminder);
        await db.SaveChangesAsync();
    }
}
