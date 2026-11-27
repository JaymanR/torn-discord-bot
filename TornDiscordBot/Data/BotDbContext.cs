using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TornDiscordBot.Data.Entities;

namespace TornDiscordBot.Data;

/// <summary>The bot's EF Core database context.</summary>
public class BotDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<RegisteredUser> RegisteredUsers => Set<RegisteredUser>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var snowflake = new ValueConverter<ulong, long>(
            id => unchecked((long)id),
            value => unchecked((ulong)value));

        modelBuilder.Entity<RegisteredUser>(entity =>
        {
            entity.HasKey(k => k.DiscordUserId);
            entity.Property(k => k.DiscordUserId)
                .HasConversion(snowflake)
                .ValueGeneratedNever();
            entity.Property(k => k.EncryptedTornApiKey).IsRequired();
        });
    }
}
