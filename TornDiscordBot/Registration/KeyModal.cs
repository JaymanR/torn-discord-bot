using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;
using TornDiscordBot.Torn;

namespace TornDiscordBot.Registration;

/// <summary>Handles submissions of the API key modal opened by <c>/register</c>.</summary>
public partial class KeyModal(IRegisteredUserRepository userRepository, ITornClient tornClient, ILogger<KeyModal> logger)
    : ComponentInteractionModule<ModalInteractionContext>
{
    /// <summary>Validates the submitted key with Torn, stores it if valid, and replies privately with the result.</summary>
    [ComponentInteraction(KeyCommands.RegisterModalId)]
    public async Task SubmitAsync()
    {
        await RespondAsync(InteractionCallback.DeferredMessage(MessageFlags.Ephemeral));

        var apiKey = Context.Components
            .OfType<Label>()
            .Select(label => label.Component)
            .OfType<TextInput>()
            .FirstOrDefault(input => input.CustomId == KeyCommands.KeyInputId)
            ?.Value.Trim();

        string reply;
        if (apiKey is null || !KeyFormat().IsMatch(apiKey))
        {
            reply = "That doesn't look like a Torn API key. Keys are 16 letters and numbers.";
        }
        else
        {
            switch (await tornClient.ValidateKeyAsync(apiKey))
            {
                case KeyValidationResult.Valid:
                    await userRepository.SaveTornApiKeyAsync(Context.User.Id, apiKey);
                    logger.LogInformation("Stored a Torn API key for Discord user {UserId}", Context.User.Id);
                    reply = "Key saved. Use `/forget` at any time to delete it.";
                    break;
                case KeyValidationResult.Invalid:
                    reply = "Torn rejected that key. Check it's correct and enabled, then try again.";
                    break;
                case KeyValidationResult.AccessLevelTooLow:
                    reply = "That key doesn't have enough access to read cooldowns. " +
                            "Create a custom key that includes the user cooldowns selection.";
                    break;
                case KeyValidationResult.Unavailable:
                default:
                    reply = "I couldn't reach Torn to check that key. Please try again in a moment.";
                    break;
            }
        }

        await FollowupAsync(new InteractionMessageProperties
        {
            Content = reply,
            Flags = MessageFlags.Ephemeral
        });
    }
    
    /// <summary>Matches the format of a Torn API key: 16 letters and digits.</summary>
    [GeneratedRegex("^[A-Za-z0-9]{16}$")]
    private static partial Regex KeyFormat();
}
