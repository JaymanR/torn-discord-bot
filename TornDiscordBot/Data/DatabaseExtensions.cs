using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace TornDiscordBot.Data;

/// <summary>Database setup for the host.</summary>
public static class DatabaseExtensions
{
    extension(IHostApplicationBuilder builder)
    {
        /// <summary>
        /// Registers the <see cref="BotDbContext"/> factory, using the SQLite file at <c>Database:Path</c>.
        /// </summary>
        public void AddDatabase()
        {
            var databasePath = builder.Configuration["Database:Path"] ?? "torndiscordbot.db";

            var directory = Path.GetDirectoryName(Path.GetFullPath(databasePath));
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            builder.Services.AddDbContextFactory<BotDbContext>(options =>
                options.UseSqlite($"Data Source={databasePath}"));
        }
    }

    extension(IHost host)
    {
        /// <summary>Applies any pending migrations.</summary>
        public async Task MigrateDatabaseAsync()
        {
            var factory = host.Services.GetRequiredService<IDbContextFactory<BotDbContext>>();
            await using var db = await factory.CreateDbContextAsync();
            await db.Database.MigrateAsync();
        }
    }
}
