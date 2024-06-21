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
    
    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UsersFriendwhitelist> UsersFriendwhitelists { get; set; }

    public virtual DbSet<UsersShocker> UsersShockers { get; set; }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseNpgsql("Host=docker-node;Port=1337;Database=discord-bot;Username=root;Password=root");
        }
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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

            entity.HasIndex(e => e.WhitelistedFriend, "friend_id").HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.Property(e => e.User).HasColumnName("user");
            entity.Property(e => e.WhitelistedFriend).HasColumnName("whitelisted_friend");
            entity.Property(e => e.CreatedOn)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_on");

            entity.HasOne(d => d.UserNavigation).WithMany(p => p.UsersFriendwhitelists)
                .HasForeignKey(d => d.User)
                .HasConstraintName("fk_user");
        });

        modelBuilder.Entity<UsersShocker>(entity =>
        {
            entity.HasKey(e => new { e.User, e.ShockerId }).HasName("users_shockers_pkey");

            entity.ToTable("users_shockers");

            entity.Property(e => e.User).HasColumnName("user");
            entity.Property(e => e.ShockerId).HasColumnName("shocker_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");

            entity.HasOne(d => d.UserNavigation).WithMany(p => p.UsersShockers)
                .HasForeignKey(d => d.User)
                .HasConstraintName("fk_user");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
