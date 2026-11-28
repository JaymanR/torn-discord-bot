using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TornDiscordBot.Interfaces;
using TornDiscordBot.Models;
using TornDiscordBot.Models.Torn;

namespace TornDiscordBot.Clients;

/// <summary>An <see cref="ITornClient"/> for Torn API v2.</summary>
public sealed class TornClient(HttpClient http, ILogger<TornClient> logger) : ITornClient
{
    /// <inheritdoc />
    public async Task<KeyValidationResult> ValidateKeyAsync(string apiKey) =>
        (await GetCooldownsAsync(apiKey)).KeyStatus;

    /// <inheritdoc />
    /// <remarks>The key is sent in the <c>Authorization</c> header and is never logged.</remarks>
    public async Task<CooldownsResult> GetCooldownsAsync(string apiKey)
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
                return root.TryGetProperty("cooldowns", out var cooldowns) &&
                       cooldowns.TryGetProperty("drug", out var drug)
                    ? new CooldownsResult(KeyValidationResult.Valid,
                        new Cooldowns(TimeSpan.FromSeconds(drug.GetInt32())))
                    : new CooldownsResult(KeyValidationResult.Unavailable);
            var code = (TornApiErrorCode)codeElement.GetInt32();
            switch (code)
            {
                case TornApiErrorCode.IncorrectKey:
                case TornApiErrorCode.ApiKeyPaused:
                    return new CooldownsResult(KeyValidationResult.Invalid);
                case TornApiErrorCode.AccessLevelTooLow:
                    return new CooldownsResult(KeyValidationResult.AccessLevelTooLow);
                case TornApiErrorCode.KeyOwnerInFederalJail:
                    return new CooldownsResult(KeyValidationResult.Valid);
                default:
                    logger.LogWarning("Torn API returned error code {Code} while reading cooldowns", (int)code);
                    return new CooldownsResult(KeyValidationResult.Unavailable);
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            logger.LogWarning("Could not read cooldowns from Torn: {ExceptionType}", ex.GetType().Name);
            return new CooldownsResult(KeyValidationResult.Unavailable);
        }
    }
}
