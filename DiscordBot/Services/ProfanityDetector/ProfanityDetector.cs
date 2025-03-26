using System.Collections.ObjectModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenShock.DiscordBot.OpenShockDiscordDb;
using System.Text;
using System.Text.RegularExpressions;

namespace OpenShock.DiscordBot.Services.ProfanityDetector;

public sealed class ProfanityDetector : IProfanityDetector
{
    private readonly OpenShockDiscordContext _db;
    private readonly ILogger<ProfanityDetector> _logger;
    
    private readonly ReaderWriterLockSlim _rulesLock = new();
    private ReadOnlyCollection<CompiledProfanityRule> _rules = new(new List<CompiledProfanityRule>());

    public ProfanityDetector(OpenShockDiscordContext db, ILogger<ProfanityDetector> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task LoadProfanityRulesAsync()
    {
        var rawRules = await _db.ProfanityRules
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
            _rules = compiledRules.AsReadOnly();
        }
        finally
        {
            _rulesLock.ExitWriteLock();
        }

        _logger.LogInformation("Loaded {Count} profanity rules.", _rules.Count);
    }

    public bool TryGetProfanityWeight(string input, out int matchCount, out float totalSeverity)
    {
        matchCount = 0;
        totalSeverity = 0f;
        
        _rulesLock.EnterReadLock();
        try
        {
            if (_rules.Count == 0 || string.IsNullOrWhiteSpace(input))
                return false;

            var normalized = input.Normalize(NormalizationForm.FormKC).ToLowerInvariant();
            var words = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet();

            foreach (var rule in _rules)
            {
                if (rule.MatchWholeWord)
                {
                    if (!words.Contains(rule.TriggerWord))
                        continue;
                }
                else
                {
                    if (!normalized.Contains(rule.TriggerWord))
                        continue;
                }

                if (rule.ValidationRegex != null && !rule.ValidationRegex.IsMatch(normalized))
                    continue;

                matchCount++;
                totalSeverity += rule.SeverityScore;
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