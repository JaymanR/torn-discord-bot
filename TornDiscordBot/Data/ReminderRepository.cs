using Microsoft.EntityFrameworkCore;
using TornDiscordBot.Entities;
using TornDiscordBot.Interfaces.Reminders;

namespace TornDiscordBot.Data;

/// <summary>An <see cref="IReminderRepository"/> backed by EF Core.</summary>
public sealed class ReminderRepository(IDbContextFactory<BotDbContext> dbFactory) : IReminderRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<ulong>> GetDueUserIdsAsync(ReminderType type, DateTimeOffset now)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        return await db.RegisteredUsers
            // ReSharper disable once AccessToDisposedClosure
            .Where(u => !db.Reminders.Any(r =>
                r.DiscordUserId == u.DiscordUserId && r.Type == type && r.NextCheckAt > now))
            .Select(u => u.DiscordUserId)
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
    public async Task SaveAsync(Reminder reminder)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        var exists = await db.Reminders.AnyAsync(r =>
            r.DiscordUserId == reminder.DiscordUserId && r.Type == reminder.Type);

        if (exists)
            db.Reminders.Update(reminder);
        else
            db.Reminders.Add(reminder);

        await db.SaveChangesAsync();
    }
}
