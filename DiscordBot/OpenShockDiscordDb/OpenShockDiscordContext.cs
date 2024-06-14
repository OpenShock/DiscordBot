using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace OpenShock.DiscordBot.OpenShockDiscordDb;

public partial class OpenShockDiscordContext : DbContext
{
    public OpenShockDiscordContext()
    {
    }

    public OpenShockDiscordContext(DbContextOptions<OpenShockDiscordContext> options)
        : base(options)
    {
    }

    public virtual DbSet<GuildActiveShocker> GuildActiveShockers { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UsersFriendwhitelist> UsersFriendwhitelists { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GuildActiveShocker>(entity =>
        {
            entity.HasKey(e => new { e.GuildId, e.DiscordId, e.ShockerId }).HasName("guild_active_shockers_pkey");

            entity.ToTable("guild_active_shockers");

            entity.Property(e => e.GuildId).HasColumnName("guild_id");
            entity.Property(e => e.DiscordId).HasColumnName("discord_id");
            entity.Property(e => e.ShockerId).HasColumnName("shocker_id");
            entity.Property(e => e.LimitDuration).HasColumnName("limit_duration");
            entity.Property(e => e.LimitIntensity).HasColumnName("limit_intensity");
            entity.Property(e => e.Paused)
                .HasDefaultValue(false)
                .HasColumnName("paused");
            entity.Property(e => e.PermShock)
                .HasDefaultValue(true)
                .HasColumnName("perm_shock");
            entity.Property(e => e.PermSound)
                .HasDefaultValue(true)
                .HasColumnName("perm_sound");
            entity.Property(e => e.PermVibrate)
                .HasDefaultValue(true)
                .HasColumnName("perm_vibrate");

            entity.HasOne(d => d.Discord).WithMany(p => p.GuildActiveShockers)
                .HasForeignKey(d => d.DiscordId)
                .HasConstraintName("discord_id");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.DiscordId).HasName("users_pkey");

            entity.ToTable("users");

            entity.HasIndex(e => e.OpenshockId, "users_openshock_id")
                .IsUnique()
                .HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.Property(e => e.DiscordId).HasColumnName("discord_id");
            entity.Property(e => e.ApiKey)
                .HasMaxLength(256)
                .HasColumnName("api_key");
            entity.Property(e => e.ApiServer)
                .HasMaxLength(256)
                .HasColumnName("api_server");
            entity.Property(e => e.CreatedOn)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_on");
            entity.Property(e => e.OpenshockId).HasColumnName("openshock_id");
        });

        modelBuilder.Entity<UsersFriendwhitelist>(entity =>
        {
            entity.HasKey(e => new { e.User, e.WhitelistedFriend }).HasName("users_friendwhitelist_pkey");

            entity.ToTable("users_friendwhitelist");

            entity.Property(e => e.User).HasColumnName("user");
            entity.Property(e => e.WhitelistedFriend).HasColumnName("whitelisted_friend");
            entity.Property(e => e.CreatedOn)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_on");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
