namespace TornDiscordBot.Data.Entities;

/// <summary>A Discord user who has registered with the bot.</summary>
public class RegisteredUser
{
    public ulong DiscordUserId { get; set; }
    public required byte[] EncryptedTornApiKey { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
