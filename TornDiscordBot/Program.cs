using Microsoft.Extensions.Hosting;

using NetCord.Hosting.Gateway;

var builder = Host.CreateApplicationBuilder(args);

var token = builder.Configuration["Discord:Token"]
    ?? throw new InvalidOperationException("Discord token is not configured.");

builder.Services
    .AddDiscordGateway(options => options.Token = token);

var host = builder.Build();

await host.RunAsync();