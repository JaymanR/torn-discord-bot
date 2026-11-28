using Microsoft.Extensions.Hosting;
using NetCord;
using NetCord.Hosting.Gateway;
using NetCord.Hosting.Services.ApplicationCommands;
using NetCord.Hosting.Services.ComponentInteractions;
using NetCord.Services.ComponentInteractions;
using TornDiscordBot.Extensions.Startup;
using TornDiscordBot.Security;

var builder = Host.CreateApplicationBuilder(args);

var token = SecretReader.Read(builder.Configuration, "Discord:Token", "/run/secrets/discord_token")
            ?? throw new InvalidOperationException(
                "Discord token is not configured. Set Discord:Token or provide /run/secrets/discord_token.");

builder.AddDatabase();
builder.AddRegistration();
builder.AddTornClient();
builder.AddMessaging();
builder.AddReminderService();

builder.Services
    .AddDiscordGateway(options => options.Token = token)
    .AddApplicationCommands()
    .AddComponentInteractions<ModalInteraction, ModalInteractionContext>();

var host = builder.Build();

await host.MigrateDatabaseAsync();

host.MapRegistrationModules();

await host.RunAsync();
