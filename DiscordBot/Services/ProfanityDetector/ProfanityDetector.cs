using System.Collections.Concurrent;
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
    
    private readonly ReaderWriterLockSlim _rulesLock = new ReaderWriterLockSlim();
    private ReadOnlyCollection<CompiledProfanityRule> _wholeWordRules = new(new List<CompiledProfanityRule>());
    private ReadOnlyCollection<CompiledProfanityRule> _containedRules =  new(new List<CompiledProfanityRule>());

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

        var wholeWord = new List<CompiledProfanityRule>();
        var contained = new List<CompiledProfanityRule>();

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

            var compiledRule = new CompiledProfanityRule
            {
                TriggerWord = rule.Trigger.ToLowerInvariant(),
                ValidationRegex = validationRegex,
                SeverityScore = rule.SeverityScore
            };

            if (rule.MatchWholeWord)
                wholeWord.Add(compiledRule);
            else
                contained.Add(compiledRule);
        }

        _rulesLock.EnterWriteLock();
        try
        {
            _wholeWordRules = wholeWord.AsReadOnly();
            _containedRules = contained.AsReadOnly();
        }
        finally
        {
            _rulesLock.ExitWriteLock();
        }

        _logger.LogInformation("Loaded {Count} profanity rules.", _wholeWordRules.Count + _containedRules.Count);
    }

    public bool TryGetProfanityWeight(string input, out int matchCount, out float totalSeverity)
    {
        matchCount = 0;
        totalSeverity = 0f;
        
        _rulesLock.EnterReadLock();
        try
        {
            if ((_wholeWordRules.Count == 0 && _containedRules.Count == 0) || string.IsNullOrWhiteSpace(input))
                return false;

            var normalized = input.Normalize(NormalizationForm.FormKC).ToLowerInvariant();
            var words = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            // Whole word rules
            foreach (var rule in _wholeWordRules)
            {
                if (!words.Contains(rule.TriggerWord))
                    continue;

                if (rule.ValidationRegex != null && !rule.ValidationRegex.IsMatch(normalized))
                    continue;

                matchCount++;
                totalSeverity += rule.SeverityScore;
            }

            // Contained rules
            foreach (var rule in _containedRules)
            {
                if (!normalized.Contains(rule.TriggerWord))
                    continue;

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
        public required string TriggerWord { get; set; }
        public required float SeverityScore { get; set; }
        public Regex? ValidationRegex { get; init; }
    }
}