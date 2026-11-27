using TornDiscordBot.Models;

namespace TornDiscordBot.Interfaces;

/// <summary>Client for the Torn API.</summary>
public interface ITornClient
{
    /// <summary>Checks that a key can read the owner's cooldowns.</summary>
    /// <param name="apiKey">The Torn API key to check.</param>
    /// <param name="ct">Cancels the request.</param>
    /// <returns>The outcome of the check.</returns>
    Task<KeyValidationResult> ValidateKeyAsync(string apiKey, CancellationToken ct = default);
}
