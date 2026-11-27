using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using TornDiscordBot.Interfaces;

namespace TornDiscordBot.Commands;

/// <summary>Slash commands for managing a user's Torn API key.</summary>
public class KeyCommands(IRegisteredUserRepository userRepository) : ApplicationCommandModule<ApplicationCommandContext>
{
    public const string RegisterModalId = "register-key";
    public const string KeyInputId = "torn-api-key";

    /// <summary>Opens a private modal where the user enters their Torn API key.</summary>
    [SlashCommand("register", "Link your Torn API key so the bot can check your cooldowns")]
    public Task RegisterAsync()
    {
        var modal = new ModalProperties(RegisterModalId, "Link your Torn API key")
        {
            Components =
            [
                new LabelProperties(
                    "Torn API key",
                    new TextInputProperties(KeyInputId, TextInputStyle.Short)
                    {
                        MinLength = 16,
                        MaxLength = 16,
                        Required = true,
                        Placeholder = "16 characters"
                    })
                {
                    Description = "Create a custom key at torn.com/preferences.php#tab=api with minimal access."
                }
            ]
        };

        return RespondAsync(InteractionCallback.Modal(modal));
    }

    /// <summary>Deletes the user's stored Torn API key.</summary>
    [SlashCommand("forget", "Delete your stored Torn API key")]
    public async Task ForgetAsync()
    {
        var deleted = await userRepository.DeleteAsync(Context.User.Id);

        await RespondAsync(InteractionCallback.Message(new InteractionMessageProperties
        {
            Content = deleted
                ? "Your API key has been deleted. You can also revoke it in your Torn API settings."
                : "You don't have a key stored.",
            Flags = MessageFlags.Ephemeral
        }));
    }
}
