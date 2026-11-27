using Microsoft.EntityFrameworkCore;
using TornDiscordBot.Data;
using TornDiscordBot.Data.Entities;
using TornDiscordBot.Security;

namespace TornDiscordBot.Registration;

/// <summary>An <see cref="IRegisteredUserRepository"/> backed by EF Core that encrypts keys before saving them.</summary>
/// <remarks>
/// Encryption is done here, not in a value converter, because it needs the owner's ID.
/// Each operation uses its own context, as this singleton may be called concurrently.
/// </remarks>
public sealed class RegisteredUserRepository(IDbContextFactory<BotDbContext> dbFactory, SecretProtector protector) : IRegisteredUserRepository
{
    /// <inheritdoc />
    public async Task SaveTornApiKeyAsync(ulong discordUserId, string apiKey)
    {
        var blob = protector.Protect(apiKey, discordUserId);

        await using var db = await dbFactory.CreateDbContextAsync();
        var existing = await db.RegisteredUsers.FindAsync([discordUserId]);

        if (existing is null)
        {
            db.RegisteredUsers.Add(new RegisteredUser
            {
                DiscordUserId = discordUserId,
                EncryptedTornApiKey = blob,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }
        else
        {
            existing.EncryptedTornApiKey = blob; // CreatedAt stays as the first registration time
        }

        await db.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<string?> GetTornApiKeyAsync(ulong discordUserId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        var blob = await db.RegisteredUsers
            .AsNoTracking()
            .Where(k => k.DiscordUserId == discordUserId)
            .Select(k => k.EncryptedTornApiKey)
            .FirstOrDefaultAsync();

        return blob is null ? null : protector.Unprotect(blob, discordUserId);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(ulong discordUserId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        return await db.RegisteredUsers
            .Where(k => k.DiscordUserId == discordUserId)
            .ExecuteDeleteAsync() > 0;
    }
}
