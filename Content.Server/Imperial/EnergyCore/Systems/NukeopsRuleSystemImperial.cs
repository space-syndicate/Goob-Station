using System.Linq;
using Content.Shared.GameTicking.Components;
using Content.Server.RoundEnd;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Imperial.EnergyCore;
using Content.Server.Imperial.EnergyCore.Events;
using Content.Server.Imperial.EnergyCore.Components;

namespace Content.Server.GameTicking.Rules;

// Imperial EnergyCore ивент подрыва ядра
public sealed partial class NukeopsRuleSystem : GameRuleSystem<NukeopsRuleComponent>
{
    private void InitializeImperial()
    {
        SubscribeLocalEvent<CoreDetonatedEvent>(OnCoreDetonated);
    }
    private void OnCoreDetonated(CoreDetonatedEvent ev)
    {
        var query = QueryActiveRules();
        while (query.MoveNext(out var uid, out _, out var nukeops, out _))
        {
            nukeops.WinConditions.Add(WinCondition.CoreExploded); // Винтайп подрыва энерго ядра
            SetWinType((uid, nukeops), WinType.OpsMajor);

            foreach (var cond in nukeops.WinConditions)
            {
                if (cond.ToString().ToLower() == "CoreExploded")
                {
                    if (!GameTicker.IsGameRuleActive("Nukeops")) return;
                    GameTicker.EndGameRule(uid);
                    _roundEndSystem.EndRound();
                    break;
                }
            }
        }
    }
}
