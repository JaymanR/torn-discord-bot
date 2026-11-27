using Microsoft.EntityFrameworkCore;
using TornDiscordBot.Data;
using TornDiscordBot.Entities;
using TornDiscordBot.Interfaces;
using TornDiscordBot.Security;

namespace TornDiscordBot.Services;

/// <summary>An <see cref="IApiKeyStore"/> backed by EF Core that encrypts keys before saving them.</summary>
/// <remarks>
/// Encryption is done here, not in a value converter, because it needs the owner's ID.
/// Each operation uses its own context, as this singleton may be called concurrently.
/// </remarks>
public sealed class ApiKeyStore(IDbContextFactory<BotDbContext> dbFactory, SecretProtector protector) : IApiKeyStore
{
    /// <inheritdoc />
    public async Task SaveAsync(ulong discordUserId, string apiKey, CancellationToken ct = default)
    {
        var blob = protector.Protect(apiKey, discordUserId);

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var existing = await db.RegisteredUsers.FindAsync([discordUserId], ct);

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

        await db.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task<string?> GetAsync(ulong discordUserId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var blob = await db.RegisteredUsers
            .AsNoTracking()
            .Where(k => k.DiscordUserId == discordUserId)
            .Select(k => k.EncryptedTornApiKey)
            .FirstOrDefaultAsync(ct);

        return blob is null ? null : protector.Unprotect(blob, discordUserId);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(ulong discordUserId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        return await db.RegisteredUsers
            .Where(k => k.DiscordUserId == discordUserId)
            .ExecuteDeleteAsync(ct) > 0;
    }
}
