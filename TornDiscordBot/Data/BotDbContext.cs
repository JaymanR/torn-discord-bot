using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TornDiscordBot.Entities;

namespace TornDiscordBot.Data;

/// <summary>The bot's EF Core database context.</summary>
public class BotDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<RegisteredUser> RegisteredUsers => Set<RegisteredUser>();
    public DbSet<Reminder> Reminders => Set<Reminder>();

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

        var unixMilliseconds = new ValueConverter<DateTimeOffset, long>(
            time => time.ToUnixTimeMilliseconds(),
            value => DateTimeOffset.FromUnixTimeMilliseconds(value));

        modelBuilder.Entity<Reminder>(entity =>
        {
            entity.HasKey(r => new { r.DiscordUserId, r.Type });
            entity.Property(r => r.DiscordUserId).HasConversion(snowflake);
            entity.Property(r => r.NextCheckAt).HasConversion(unixMilliseconds);
            entity.HasOne<RegisteredUser>()
                .WithMany()
                .HasForeignKey(r => r.DiscordUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
