using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenShock.DiscordBot.OpenShockDiscordDb;
using System.Text;
using System.Text.RegularExpressions;

namespace OpenShock.DiscordBot.Services.ProfanityDetector;

public sealed class ProfanityDetector : IProfanityDetector
{
    private readonly IDbContextFactory<OpenShockDiscordContext> _dbFactory;
    private readonly ILogger<ProfanityDetector> _logger;
    
    private readonly ReaderWriterLockSlim _rulesLock = new();
    private ImmutableArray<CompiledProfanityRule> _rules = [];

    public ProfanityDetector(IDbContextFactory<OpenShockDiscordContext> dbFactory, ILogger<ProfanityDetector> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public async Task LoadProfanityRulesAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        
        var rawRules = await db.ProfanityRules
            .Where(r => r.IsActive)
            .ToListAsync();

        var compiledRules = new List<CompiledProfanityRule>();

        foreach (var rule in rawRules)
        {
            Regex? validationRegex = null;

            if (!string.IsNullOrEmpty(rule.ValidationRegex))
            {
                try
                {
                    validationRegex = new Regex(rule.ValidationRegex, RegexOptions.IgnoreCase | RegexOptions.Compiled);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Invalid regex for trigger '{Trigger}'", rule.Trigger);
                    continue;
                }
            }
            else if (!rule.MatchWholeWord)
            {
                validationRegex = new Regex(Regex.Escape(rule.Trigger), RegexOptions.IgnoreCase | RegexOptions.Compiled);
            }

            compiledRules.Add(new CompiledProfanityRule
            {
                TriggerWord = rule.Trigger.ToLowerInvariant(),
                MatchWholeWord = rule.MatchWholeWord,
                ValidationRegex = validationRegex,
                SeverityScore = rule.SeverityScore
            });
        }

        _rulesLock.EnterWriteLock();
        try
        {
            _rules = [..compiledRules];
        }
        finally
        {
            _rulesLock.ExitWriteLock();
        }

        _logger.LogInformation("Loaded {Count} profanity rules.", _rules.Length);
    }

    public bool TryGetProfanityWeight(string input, out int matchCount, out float totalSeverity)
    {
        matchCount = 0;
        totalSeverity = 0f;
        
        _rulesLock.EnterReadLock();
        try
        {
            if (_rules.Length <= 0 || string.IsNullOrWhiteSpace(input))
                return false;

            var normalized = input.Normalize(NormalizationForm.FormKC).ToLowerInvariant();
            var words = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet();

            foreach (var rule in _rules)
            {
                int count = 0;
                
                if (rule.MatchWholeWord)
                {
                    count = words.Count(w => w == rule.TriggerWord);
                }
                else
                {
                    if (!normalized.Contains(rule.TriggerWord)) continue;

                    count = rule.ValidationRegex?.Matches(input).Count ?? 1; // Really shouldnt happpen but yeah
                }

                if (count <= 0) continue;

                matchCount += count;
                totalSeverity += count * rule.SeverityScore;
            }

            return matchCount > 0;
        }
        finally
        {
            _rulesLock.ExitReadLock();
        }
    }

    private sealed class CompiledProfanityRule
    {
        public required string TriggerWord { get; init; }
        public bool MatchWholeWord { get; init; }
        public required float SeverityScore { get; init; }
        public Regex? ValidationRegex { get; init; }
    }
}