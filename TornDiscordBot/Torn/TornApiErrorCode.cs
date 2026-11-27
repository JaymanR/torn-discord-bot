namespace TornDiscordBot.Torn;

/// <summary>
/// The subset of Torn API error codes the bot reacts to
/// (see the "Error*" schemas in Torn's OpenAPI spec for the full list).
/// </summary>
public enum TornApiErrorCode
{
    IncorrectKey = 2,
    KeyOwnerInFederalJail = 10,
    AccessLevelTooLow = 16,
    ApiKeyPaused = 18
}
