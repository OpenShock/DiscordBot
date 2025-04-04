﻿using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using OpenShock.DiscordBot.OpenShockDiscordDb;
using OpenShock.DiscordBot.Utils;
using System.Collections.Frozen;
using System.Globalization;
using System.Text;

namespace OpenShock.DiscordBot.Commands.Profanity;

public sealed partial class ProfanityGroup
{
    public static readonly FrozenDictionary<string, string> RelevantCultures =
        CultureInfo.GetCultures(CultureTypes.NeutralCultures).Where(c => c.EnglishName.All(char.IsLetter)).ToFrozenDictionary(c => c.EnglishName.ToLowerInvariant(), c => c.Name);

    [SlashCommand("suggest", "Suggest a new trigger (word or phrase) to be detected as profanity.")]
    public async Task ProfanitySuggestCommand(string trigger, string comment, string language)
    {
        bool ephemeral = Context.IsNotDm();

        await DeferAsync(ephemeral: ephemeral);

        if (string.IsNullOrWhiteSpace(trigger) || string.IsNullOrWhiteSpace(comment) || string.IsNullOrWhiteSpace(language))
        {
            await FollowupAsync("❌ All fields are required.", ephemeral: ephemeral);
            return;
        }

        trigger = trigger.Normalize(NormalizationForm.FormKC).Trim().ToLowerInvariant();
        comment = comment.Trim();

        if (!RelevantCultures.TryGetValue(language.Trim().ToLowerInvariant(), out var languageCode))
        {
            await FollowupAsync($"❌ {language} is not a valid language.", ephemeral: ephemeral);
            return;
        }

        var suggestion = await _db.ProfanitySuggestions.FirstOrDefaultAsync(r => r.Trigger == trigger);
        if (suggestion != null)
        {
            await FollowupAsync($"✅ {trigger} has already been suggested and is pending a review.", ephemeral: ephemeral);
            return;
        }

        var rejection = await _db.RejectedProfanitySuggestions.FirstOrDefaultAsync(r => r.Trigger == trigger);
        if (rejection != null)
        {
            await FollowupAsync($"❌ {trigger} has already been suggested and rejected: {rejection.Reason}.", ephemeral: ephemeral);
            return;
        }

        suggestion = new ProfanitySuggestion
        {
            Trigger = trigger,
            Comment = comment,
            LanguageCode = languageCode,
            SuggestedByUserId = Context.User.Id,
            SuggestedAt = DateTimeOffset.UtcNow
        };

        _db.ProfanitySuggestions.Add(suggestion);
        await _db.SaveChangesAsync();

        await FollowupAsync("✅ Your suggestion has been submitted for review. Thank you!", ephemeral: ephemeral);
    }
}