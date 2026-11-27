using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace TornDiscordBot.Torn;

/// <summary>Setup for Torn API access.</summary>
public static class TornExtensions
{
    extension(IHostApplicationBuilder builder)
    {
        /// <summary>Registers the <see cref="ITornClient"/> and its HTTP client.</summary>
        public void AddTornClient()
        {
            builder.Services.AddHttpClient<ITornClient, TornClient>(client =>
            {
                client.BaseAddress = new Uri("https://api.torn.com/v2/");
                client.Timeout = TimeSpan.FromSeconds(15);
            });
        }
    }
}
