using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace TornDiscordBot.Torn;

/// <summary>An <see cref="ITornClient"/> for Torn API v2.</summary>
public sealed class TornClient(HttpClient http, ILogger<TornClient> logger) : ITornClient
{
    /// <inheritdoc />
    /// <remarks>The key is sent in the <c>Authorization</c> header and is never logged.</remarks>
    public async Task<KeyValidationResult> ValidateKeyAsync(string apiKey)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "user/cooldowns");
            request.Headers.Authorization = new AuthenticationHeaderValue("ApiKey", apiKey);

            using var response = await http.SendAsync(request);
            await using var body = await response.Content.ReadAsStreamAsync();
            using var json = await JsonDocument.ParseAsync(body);
            var root = json.RootElement;

            if (!root.TryGetProperty("error", out var error) || !error.TryGetProperty("code", out var codeElement))
                return root.TryGetProperty("cooldowns", out _)
                    ? KeyValidationResult.Valid
                    : KeyValidationResult.Unavailable;
            var code = (TornApiErrorCode)codeElement.GetInt32();
            switch (code)
            {
                case TornApiErrorCode.IncorrectKey:
                case TornApiErrorCode.ApiKeyPaused:
                    return KeyValidationResult.Invalid;
                case TornApiErrorCode.AccessLevelTooLow:
                    return KeyValidationResult.AccessLevelTooLow;
                case TornApiErrorCode.KeyOwnerInFederalJail:
                    return KeyValidationResult.Valid;
                default:
                    logger.LogWarning("Torn API returned error code {Code} while validating a key", (int)code);
                    return KeyValidationResult.Unavailable;
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            logger.LogWarning("Could not validate key with Torn: {ExceptionType}", ex.GetType().Name);
            return KeyValidationResult.Unavailable;
        }
    }
}
