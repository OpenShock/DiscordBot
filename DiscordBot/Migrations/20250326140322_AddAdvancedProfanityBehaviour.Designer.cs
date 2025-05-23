﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using OpenShock.DiscordBot.OpenShockDiscordDb;

#nullable disable

namespace OpenShock.DiscordBot.Migrations
{
    [DbContext(typeof(OpenShockDiscordContext))]
    [Migration("20250326140322_AddAdvancedProfanityBehaviour")]
    partial class AddAdvancedProfanityBehaviour
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.2")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("OpenShock.DiscordBot.OpenShockDiscordDb.BotAdmin", b =>
                {
                    b.Property<decimal>("DiscordId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("discord_id");

                    b.Property<DateTime>("CreatedOn")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_on")
                        .HasDefaultValueSql("CURRENT_TIMESTAMP");

                    b.Property<bool>("IsRemovable")
                        .HasColumnType("boolean")
                        .HasColumnName("is_removable");

                    b.HasKey("DiscordId")
                        .HasName("administrators_pkey");

                    b.ToTable("administrators", (string)null);
                });

            modelBuilder.Entity("OpenShock.DiscordBot.OpenShockDiscordDb.ProfanityRule", b =>
                {
                    b.Property<decimal>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("id");

                    b.Property<decimal?>("AddedByUserId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("added_by_user_id");

                    b.Property<string>("Category")
                        .HasMaxLength(32)
                        .HasColumnType("character varying(32)")
                        .HasColumnName("category");

                    b.Property<string>("Comment")
                        .HasMaxLength(512)
                        .HasColumnType("character varying(512)")
                        .HasColumnName("comment");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_at")
                        .HasDefaultValueSql("CURRENT_TIMESTAMP");

                    b.Property<bool>("IsActive")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasDefaultValue(true)
                        .HasColumnName("is_active");

                    b.Property<string>("LanguageCode")
                        .IsRequired()
                        .HasMaxLength(16)
                        .HasColumnType("character varying(16)")
                        .HasColumnName("language_code");

                    b.Property<bool>("MatchWholeWord")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasDefaultValue(true)
                        .HasColumnName("match_whole_word");

                    b.Property<float>("SeverityScore")
                        .HasColumnType("real")
                        .HasColumnName("severity_score");

                    b.Property<string>("Trigger")
                        .IsRequired()
                        .HasMaxLength(128)
                        .HasColumnType("character varying(128)")
                        .HasColumnName("trigger");

                    b.Property<string>("ValidationRegex")
                        .HasMaxLength(512)
                        .HasColumnType("character varying(512)")
                        .HasColumnName("validation_regex");

                    b.HasKey("Id")
                        .HasName("profanity_rules_pkey");

                    b.ToTable("profanity_rules", (string)null);
                });

            modelBuilder.Entity("OpenShock.DiscordBot.OpenShockDiscordDb.ProfanitySuggestion", b =>
                {
                    b.Property<decimal>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("id");

                    b.Property<string>("Comment")
                        .IsRequired()
                        .HasMaxLength(512)
                        .HasColumnType("character varying(512)")
                        .HasColumnName("comment");

                    b.Property<string>("LanguageCode")
                        .IsRequired()
                        .HasMaxLength(16)
                        .HasColumnType("character varying(16)")
                        .HasColumnName("language_code");

                    b.Property<DateTimeOffset>("SuggestedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("suggested_at")
                        .HasDefaultValueSql("CURRENT_TIMESTAMP");

                    b.Property<decimal>("SuggestedByUserId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("suggested_by_user_id");

                    b.Property<string>("Trigger")
                        .IsRequired()
                        .HasMaxLength(128)
                        .HasColumnType("character varying(128)")
                        .HasColumnName("trigger");

                    b.HasKey("Id")
                        .HasName("profanity_suggestions_pkey");

                    b.ToTable("profanity_suggestions", (string)null);
                });

            modelBuilder.Entity("OpenShock.DiscordBot.OpenShockDiscordDb.RejectedProfanitySuggestion", b =>
                {
                    b.Property<decimal>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("id");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_at")
                        .HasDefaultValueSql("CURRENT_TIMESTAMP");

                    b.Property<string>("LanguageCode")
                        .IsRequired()
                        .HasMaxLength(16)
                        .HasColumnType("character varying(16)")
                        .HasColumnName("language_code");

                    b.Property<string>("Reason")
                        .IsRequired()
                        .HasMaxLength(512)
                        .HasColumnType("character varying(512)")
                        .HasColumnName("reason");

                    b.Property<decimal>("SuggestedByUserId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("suggested_by_user_id");

                    b.Property<string>("Trigger")
                        .IsRequired()
                        .HasMaxLength(128)
                        .HasColumnType("character varying(128)")
                        .HasColumnName("trigger");

                    b.HasKey("Id")
                        .HasName("rejected_profanity_suggestions_pkey");

                    b.ToTable("rejected_profanity_suggestions", (string)null);
                });

            modelBuilder.Entity("OpenShock.DiscordBot.OpenShockDiscordDb.User", b =>
                {
                    b.Property<decimal>("DiscordId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("discord_id");

                    b.Property<string>("ApiKey")
                        .IsRequired()
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)")
                        .HasColumnName("api_key");

                    b.Property<string>("ApiServer")
                        .IsRequired()
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)")
                        .HasColumnName("api_server");

                    b.Property<DateTime>("CreatedOn")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_on")
                        .HasDefaultValueSql("CURRENT_TIMESTAMP");

                    b.Property<Guid>("OpenshockId")
                        .HasColumnType("uuid")
                        .HasColumnName("openshock_id");

                    b.Property<bool>("ProfanityShocking")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasColumnName("profanity_shocking")
                        .HasDefaultValueSql("false");

                    b.HasKey("DiscordId")
                        .HasName("users_pkey");

                    b.HasIndex(new[] { "OpenshockId" }, "users_openshock_id")
                        .IsUnique()
                        .HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

                    b.ToTable("users", (string)null);
                });

            modelBuilder.Entity("OpenShock.DiscordBot.OpenShockDiscordDb.UsersFriendwhitelist", b =>
                {
                    b.Property<decimal>("User")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("user");

                    b.Property<decimal>("WhitelistedFriend")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("whitelisted_friend");

                    b.Property<DateTime>("CreatedOn")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_on")
                        .HasDefaultValueSql("CURRENT_TIMESTAMP");

                    b.HasKey("User", "WhitelistedFriend")
                        .HasName("users_friendwhitelist_pkey");

                    b.HasIndex(new[] { "WhitelistedFriend" }, "friend_id")
                        .HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

                    b.ToTable("users_friendwhitelist", (string)null);
                });

            modelBuilder.Entity("OpenShock.DiscordBot.OpenShockDiscordDb.UsersShocker", b =>
                {
                    b.Property<decimal>("User")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("user");

                    b.Property<Guid>("ShockerId")
                        .HasColumnType("uuid")
                        .HasColumnName("shocker_id");

                    b.Property<DateTime>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_at")
                        .HasDefaultValueSql("CURRENT_TIMESTAMP");

                    b.HasKey("User", "ShockerId")
                        .HasName("users_shockers_pkey");

                    b.ToTable("users_shockers", (string)null);
                });

            modelBuilder.Entity("OpenShock.DiscordBot.OpenShockDiscordDb.UsersFriendwhitelist", b =>
                {
                    b.HasOne("OpenShock.DiscordBot.OpenShockDiscordDb.User", "UserNavigation")
                        .WithMany("UsersFriendwhitelists")
                        .HasForeignKey("User")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_user");

                    b.Navigation("UserNavigation");
                });

            modelBuilder.Entity("OpenShock.DiscordBot.OpenShockDiscordDb.UsersShocker", b =>
                {
                    b.HasOne("OpenShock.DiscordBot.OpenShockDiscordDb.User", "UserNavigation")
                        .WithMany("UsersShockers")
                        .HasForeignKey("User")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_user");

                    b.Navigation("UserNavigation");
                });

            modelBuilder.Entity("OpenShock.DiscordBot.OpenShockDiscordDb.User", b =>
                {
                    b.Navigation("UsersFriendwhitelists");

                    b.Navigation("UsersShockers");
                });
#pragma warning restore 612, 618
        }
    }
}
