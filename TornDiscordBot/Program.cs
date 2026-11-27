using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NetCord;
using NetCord.Hosting.Gateway;
using NetCord.Hosting.Services;
using NetCord.Hosting.Services.ApplicationCommands;
using NetCord.Hosting.Services.ComponentInteractions;
using NetCord.Services.ComponentInteractions;
using TornDiscordBot.Commands;
using TornDiscordBot.Data;
using TornDiscordBot.Interfaces;
using TornDiscordBot.Security;
using TornDiscordBot.Services;

var builder = Host.CreateApplicationBuilder(args);

// Secrets come from configuration (env vars / user-secrets for local dev) or Docker secret files.
var token = SecretReader.Read(builder.Configuration, "Discord:Token", "/run/secrets/discord_token")
            ?? throw new InvalidOperationException(
                "Discord token is not configured. Set Discord:Token or provide /run/secrets/discord_token.");

var masterKey = SecretReader.Read(builder.Configuration, "Security:MasterKey", "/run/secrets/master_key")
                ?? throw new InvalidOperationException(
                    "Master key is not configured. Set Security:MasterKey or provide /run/secrets/master_key " +
                    "(generate one with: openssl rand -base64 32).");

var databasePath = builder.Configuration["Database:Path"] ?? "torndiscordbot.db";
var protector = SecretProtector.FromBase64(masterKey);

var databaseDirectory = Path.GetDirectoryName(Path.GetFullPath(databasePath));
if (!string.IsNullOrEmpty(databaseDirectory))
    Directory.CreateDirectory(databaseDirectory);

builder.Services.AddSingleton(protector);
builder.Services.AddDbContextFactory<BotDbContext>(options => options.UseSqlite($"Data Source={databasePath}"));
builder.Services.AddSingleton<IApiKeyStore, ApiKeyStore>();
builder.Services.AddHttpClient<ITornClient, TornClient>(client =>
{
    client.BaseAddress = new Uri("https://api.torn.com/v2/");
    client.Timeout = TimeSpan.FromSeconds(15);
});

builder.Services.AddSingleton<IMessageSenderService, MessageSenderService>();
builder.Services
    .AddDiscordGateway(options => options.Token = token)
    .AddApplicationCommands()
    .AddComponentInteractions<ModalInteraction, ModalInteractionContext>();

var host = builder.Build();

// Apply any pending schema migrations before the bot starts handling commands.
await using (var db = await host.Services.GetRequiredService<IDbContextFactory<BotDbContext>>().CreateDbContextAsync())
{
    await db.Database.MigrateAsync();
}

host.AddApplicationCommandModule<KeyCommands>();
host.AddComponentInteractionModule<KeyModal>();

var sender = host.Services.GetRequiredService<IMessageSenderService>();
await sender.SendAsync(1574807747135013004, "hello world");


await host.RunAsync();