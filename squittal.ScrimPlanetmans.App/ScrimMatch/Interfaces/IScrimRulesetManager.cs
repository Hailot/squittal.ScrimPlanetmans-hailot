using squittal.ScrimPlanetmans.ScrimMatch.Messages;
using squittal.ScrimPlanetmans.ScrimMatch.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace squittal.ScrimPlanetmans.ScrimMatch
{
    public interface IScrimRulesetManager
    {
        Ruleset ActiveRuleset { get; }

        Task<Ruleset> GetActiveRulesetAsync(bool forceRefresh = false);
        Task<Ruleset> GetDefaultRulesetAsync();

        Task<Ruleset> ActivateRulesetAsync(int rulesetId);
        Task SetUpActiveRulesetAsync();

        Task SeedDefaultRuleset();
        Task SeedScrimActionModels();
        Task<IEnumerable<Ruleset>> GetRulesetsAsync(CancellationToken cancellationToken);
        Task<Ruleset> ActivateDefaultRulesetAsync();
    }
}
