using TornDiscordBot.Models;
using TornDiscordBot.Models.Torn;

namespace TornDiscordBot.Interfaces;

/// <summary>Client for the Torn API.</summary>
public interface ITornClient
{
    /// <summary>Checks that a key can read the owner's cooldowns.</summary>
    /// <param name="apiKey">The Torn API key to check.</param>
    /// <returns>The outcome of the check.</returns>
    Task<KeyValidationResult> ValidateKeyAsync(string apiKey);

    /// <summary>Gets the cooldowns of the key's owner.</summary>
    /// <param name="apiKey">The owner's Torn API key.</param>
    /// <returns>The cooldowns if Torn returned them, with the status of the key.</returns>
    Task<CooldownsResult> GetCooldownsAsync(string apiKey);
}
