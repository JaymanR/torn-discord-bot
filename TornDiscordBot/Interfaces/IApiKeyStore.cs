namespace TornDiscordBot.Interfaces;

/// <summary>Persists users' Torn API keys.</summary>
public interface IApiKeyStore
{
    /// <summary>Stores the user's Torn API key encrypted, replacing any existing key.</summary>
    /// <param name="discordUserId">The Discord user who owns the key.</param>
    /// <param name="apiKey">The plaintext Torn API key.</param>
    /// <param name="ct">Cancels the operation.</param>
    Task SaveAsync(ulong discordUserId, string apiKey, CancellationToken ct = default);

    /// <summary>Gets the user's Torn API key.</summary>
    /// <param name="discordUserId">The Discord user who owns the key.</param>
    /// <param name="ct">Cancels the operation.</param>
    /// <returns>The decrypted key, or <see langword="null"/> if none is stored.</returns>
    Task<string?> GetAsync(ulong discordUserId, CancellationToken ct = default);

    /// <summary>Deletes the user's Torn API key.</summary>
    /// <param name="discordUserId">The Discord user who owns the key.</param>
    /// <param name="ct">Cancels the operation.</param>
    /// <returns><see langword="true"/> if a key was deleted; <see langword="false"/> if none was stored.</returns>
    Task<bool> DeleteAsync(ulong discordUserId, CancellationToken ct = default);
}
