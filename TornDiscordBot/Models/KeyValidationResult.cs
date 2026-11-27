namespace TornDiscordBot.Models;

/// <summary>The outcome of checking a Torn API key.</summary>
public enum KeyValidationResult
{
    /// <summary>Key works and can read cooldowns.</summary>
    Valid,

    /// <summary>Torn says the key does not exist or is disabled.</summary>
    Invalid,

    /// <summary>Key exists but lacks the access needed to read cooldowns.</summary>
    AccessLevelTooLow,

    /// <summary>Torn could not be reached or returned something unexpected; try again later.</summary>
    Unavailable
}
