using Microsoft.Extensions.Configuration;

namespace TornDiscordBot.Security;

/// <summary>Loads secrets needed at startup.</summary>
public static class SecretReader
{
    /// <summary>Reads a secret from configuration, falling back to a Docker secret file.</summary>
    /// <param name="config">Application configuration, including environment variables and user-secrets.</param>
    /// <param name="configKey">The configuration key to check first.</param>
    /// <param name="dockerSecretPath">The secret file to read if the key is not set.</param>
    /// <returns>The trimmed secret, or <see langword="null"/> if neither source has a value.</returns>
    public static string? Read(IConfiguration config, string configKey, string dockerSecretPath)
    {
        var value = config[configKey];
        if (!string.IsNullOrWhiteSpace(value))
            return value.Trim();

        if (!File.Exists(dockerSecretPath)) return null;
        value = File.ReadAllText(dockerSecretPath).Trim();
        return value.Length > 0 ? value : null;
    }
}
