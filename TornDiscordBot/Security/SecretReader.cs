using Microsoft.Extensions.Configuration;

namespace TornDiscordBot.Security;

/// <summary>Loads secrets needed at startup.</summary>
public static class SecretReader
{
    /// <summary>Reads a secret from configuration, falling back to a Docker secret file.</summary>
    /// <param name="configuration">Application configuration, including environment variables and user-secrets.</param>
    /// <param name="configKey">The configuration key to check first.</param>
    /// <param name="dockerSecretPath">The secret file to read if the key is not set.</param>
    /// <returns>The trimmed secret, or <see langword="null"/> if neither source has a value.</returns>
    public static string? Read(IConfiguration configuration, string configKey, string dockerSecretPath)
    {
        var value = configuration[configKey];
        if (!string.IsNullOrWhiteSpace(value))
            return value.Trim();

        if (File.Exists(dockerSecretPath))
        {
            value = File.ReadAllText(dockerSecretPath).Trim();
            if (value.Length > 0)
                return value;
        }

        return null;
    }
}
