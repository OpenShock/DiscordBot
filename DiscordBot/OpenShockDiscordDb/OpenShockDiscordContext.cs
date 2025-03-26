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

    public virtual DbSet<ProfanityRule> ProfanityRules { get; set; }

    public virtual DbSet<ProfanitySuggestion> ProfanitySuggestions { get; set; }

    public virtual DbSet<RejectedProfanitySuggestion> RejectedProfanitySuggestions { get; set; }

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
            entity.Property(e => e.ProfanityShocking)
                .HasDefaultValueSql("false")
                .HasColumnName("profanity_shocking");
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

        modelBuilder.Entity<ProfanityRule>(entity =>
        {
            entity.ToTable("profanity_rules");

            entity.HasKey(e => e.Id).HasName("profanity_rules_pkey");

            entity.Property(e => e.Id).HasColumnName("id");

            entity.Property(e => e.Trigger)
                .IsRequired()
                .HasMaxLength(128)
                .HasColumnName("trigger");

            entity.Property(e => e.SeverityScore)
                .HasColumnName("severity_score");

            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");

            entity.Property(e => e.MatchWholeWord)
                .HasDefaultValue(true)
                .HasColumnName("match_whole_word");

            entity.Property(e => e.ValidationRegex)
                .HasMaxLength(512)
                .HasColumnName("validation_regex");

            entity.Property(e => e.LanguageCode)
                .IsRequired()
                .HasMaxLength(16)
                .HasColumnName("language_code");

            entity.Property(e => e.Category)
                .HasMaxLength(32)
                .HasColumnName("category");

            entity.Property(e => e.Comment)
                .HasMaxLength(512)
                .HasColumnName("comment");

            entity.Property(e => e.AddedByUserId)
                .HasColumnName("added_by_user_id");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
        });

        modelBuilder.Entity<ProfanitySuggestion>(entity =>
        {
            entity.ToTable("profanity_suggestions");

            entity.HasKey(e => e.Id).HasName("profanity_suggestions_pkey");

            entity.Property(e => e.Id).HasColumnName("id");

            entity.Property(e => e.Trigger)
                .IsRequired()
                .HasMaxLength(128)
                .HasColumnName("trigger");

            entity.Property(e => e.Comment)
                .HasMaxLength(512)
                .HasColumnName("comment");

            entity.Property(e => e.LanguageCode)
                .IsRequired()
                .HasMaxLength(16)
                .HasColumnName("language_code");

            entity.Property(e => e.SuggestedByUserId)
                .HasColumnName("suggested_by_user_id");

            entity.Property(e => e.SuggestedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("suggested_at");
        });

        modelBuilder.Entity<RejectedProfanitySuggestion>(entity =>
        {
            entity.ToTable("rejected_profanity_suggestions");

            entity.HasKey(e => e.Id).HasName("rejected_profanity_suggestions_pkey");

            entity.Property(e => e.Id)
                .HasColumnName("id");

            entity.Property(e => e.Trigger)
                .IsRequired()
                .HasMaxLength(128)
                .HasColumnName("trigger");

            entity.Property(e => e.Reason)
                .IsRequired()
                .HasMaxLength(512)
                .HasColumnName("reason");

            entity.Property(e => e.LanguageCode)
                .IsRequired()
                .HasMaxLength(16)
                .HasColumnName("language_code");

            entity.Property(e => e.SuggestedByUserId)
                .HasColumnName("suggested_by_user_id");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
