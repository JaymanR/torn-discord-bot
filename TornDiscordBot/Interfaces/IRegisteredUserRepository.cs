namespace TornDiscordBot.Interfaces;

/// <summary>Persists registered users and their Torn API keys.</summary>
public interface IRegisteredUserRepository
{
    /// <summary>Stores the user's Torn API key encrypted, registering them or replacing their existing key.</summary>
    /// <param name="discordUserId">The Discord user who owns the key.</param>
    /// <param name="apiKey">The plaintext Torn API key.</param>
    Task SaveTornApiKeyAsync(ulong discordUserId, string apiKey);

    /// <summary>Gets the user's Torn API key.</summary>
    /// <param name="discordUserId">The Discord user who owns the key.</param>
    /// <returns>The decrypted key, or <see langword="null"/> if none is stored.</returns>
    Task<string?> GetTornApiKeyAsync(ulong discordUserId);

    /// <summary>Checks whether the user has registered a torn api key.</summary>
    /// <param name="discordUserId">The Discord user to look for.</param>
    /// <returns><see langword="true"/> if the user is registered.</returns>
    Task<bool> IsRegisteredAsync(ulong discordUserId);

    /// <summary>Deletes the user, including their stored Torn API key.</summary>
    /// <param name="discordUserId">The Discord user who owns the key.</param>
    /// <returns><see langword="true"/> if a user was deleted; <see langword="false"/> if they weren't registered.</returns>
    Task<bool> DeleteAsync(ulong discordUserId);
}
