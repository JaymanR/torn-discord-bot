namespace TornDiscordBot.Models.Torn;

/// <summary>A player's cooldowns.</summary>
/// <param name="Drug">Time left on the drug cooldown, or zero if there is none.</param>
public sealed record Cooldowns(TimeSpan Drug);
