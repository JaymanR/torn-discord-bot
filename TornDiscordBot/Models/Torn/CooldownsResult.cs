namespace TornDiscordBot.Models.Torn;

/// <summary>The response to a request for a player's cooldowns.</summary>
/// <param name="KeyStatus">What the request showed about the API key.</param>
/// <param name="Cooldowns">The cooldowns, or <see langword="null"/> if Torn didn't return them.</param>
public sealed record CooldownsResult(KeyValidationResult KeyStatus, Cooldowns? Cooldowns = null);
