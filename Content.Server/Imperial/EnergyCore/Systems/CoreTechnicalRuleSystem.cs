using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.GameTicking.Components;
using System.Linq;
using Robust.Shared.Prototypes;
using Content.Server.Imperial.EnergyCore.Components;
using Content.Server.Imperial.EnergyCore.Events;

namespace Content.Server.Imperial.EnergyCore;

public sealed class CoreTechnicalRuleSystem : GameRuleSystem<CoreTechnicalRuleComponent>
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CoreDetonatedEvent>(OnCoreDetonated);
    }

    protected override void Started(EntityUid uid,
        CoreTechnicalRuleComponent component,
        GameRuleComponent gameRule,
        GameRuleStartedEvent args)
    {
    }

    protected override void AppendRoundEndText(EntityUid uid,
        CoreTechnicalRuleComponent component,
        GameRuleComponent gameRule,
        ref RoundEndTextAppendEvent args)
    {
        var coremain = Loc.GetString("core-technical-main");
        args.AddLine(coremain);

        var text = component.EndRoundText;
        args.AddLine(text);
    }
    private void OnCoreDetonated(CoreDetonatedEvent ev)
    {
        var query = QueryActiveRules();
        while (query.MoveNext(out var uid, out _, out var core, out _))
        {
            if (GameTicker.IsGameRuleActive("CoreTechnical"))
            {
                GameTicker.EndGameRule(uid);
            }

        }
    }
}
