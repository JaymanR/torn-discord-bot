using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NetCord.Hosting.Gateway;
using TornDiscordBot.Interfaces;
using TornDiscordBot.Services;

var builder = Host.CreateApplicationBuilder(args);

var token = builder.Configuration["Discord:Token"]
            ?? throw new InvalidOperationException("Discord token is not configured.");

builder.Services.AddSingleton<IMessageSenderService, MessageSenderService>();
builder.Services.AddDiscordGateway(options => options.Token = token);

var host = builder.Build();

var sender = host.Services.GetRequiredService<IMessageSenderService>();
await sender.SendAsync(1574807747135013004, "hello world");


await host.RunAsync();