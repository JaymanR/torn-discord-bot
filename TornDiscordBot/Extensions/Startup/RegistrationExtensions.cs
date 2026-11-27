using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NetCord.Hosting.Services.ApplicationCommands;
using NetCord.Hosting.Services.ComponentInteractions;
using TornDiscordBot.Commands;
using TornDiscordBot.Data;
using TornDiscordBot.Interfaces;
using TornDiscordBot.Security;

namespace TornDiscordBot.Extensions.Startup;

/// <summary>Setup for the registration feature.</summary>
public static class RegistrationExtensions
{
    extension(IHostApplicationBuilder builder)
    {
        /// <summary>Registers the user repository and the encryption it depends on.</summary>
        /// <exception cref="InvalidOperationException">The master key is missing or invalid.</exception>
        public void AddRegistration()
        {
            var masterKey = SecretReader.Read(builder.Configuration, "Security:MasterKey", "/run/secrets/master_key")
                            ?? throw new InvalidOperationException(
                                "Master key is not configured. Set Security:MasterKey or provide " +
                                "/run/secrets/master_key (generate one with: openssl rand -base64 32).");

            builder.Services.AddSingleton(SecretProtector.FromBase64(masterKey));
            builder.Services.AddSingleton<IRegisteredUserRepository, RegisteredUserRepository>();
        }
    }

    extension(IHost host)
    {
        /// <summary>Adds the registration slash commands and modal handler.</summary>
        public void MapRegistrationModules()
        {
            host.AddApplicationCommandModule<KeyCommands>();
            host.AddComponentInteractionModule<KeyModal>();
        }
    }
}
