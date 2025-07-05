using Content.Server.Objectives.Components;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Server.GameTicking.Rules;
using Content.Server.Revolutionary.Components;
using Robust.Shared.Random;
using System.Linq;
using Content.Server.Objectives.Components;
using Content.Server.Objectives.Systems;
using Content.Server.Imperial.NinjaMultitask.Components;
namespace Content.Server.Imperial.NinjaMultitask.Systems;

public sealed class PickRandomSyndicateTargetSystem : EntitySystem
{
    [Dependency] private readonly TargetObjectiveSystem _target = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly TraitorRuleSystem _traitorRule = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PickRandomSyndicateTargetComponent, ObjectiveAssignedEvent>(OnRandomSyndicateTargetAssigned);
    }


    private void OnRandomSyndicateTargetAssigned(Entity<PickRandomSyndicateTargetComponent> ent, ref ObjectiveAssignedEvent args)
    {
        if (!TryComp<TargetObjectiveComponent>(ent.Owner, out var target))
        {
            args.Cancelled = true;
            return;
        }
        var traitors = _traitorRule.GetOtherTraitorMindsAliveAndConnected(args.Mind).ToHashSet();
        var targets = new HashSet<Entity<MindComponent>>();
        foreach (var traitor in traitors)
        {
            if (!TryComp<MindComponent>(traitor.Id, out var mind))
                continue;

            foreach (var objective in mind.Objectives)
            {
                if (HasComp<KillPersonConditionComponent>(objective) && TryComp<TargetObjectiveComponent>(objective, out var targetComp))
                {
                    var targt = targetComp.Target ?? EntityUid.Invalid;
                    if (TryComp<MindComponent>(targetComp.Target, out var mindt))
                        targets.Add(new Entity<MindComponent>(targt, mindt));
                }
            }
        }
        var isEmpty = (targets.Count == 0);
        if (!isEmpty)
        {
            _target.SetTarget(ent.Owner, _random.Pick(targets), target);
        }
        else
        {
            var allHumans = _mind.GetAliveHumans(args.MindId);

            // Can't have multiple objectives to kill the same person
            foreach (var objective in args.Mind.Objectives)
            {
                if (HasComp<KeepAliveConditionComponent>(objective) && TryComp<TargetObjectiveComponent>(objective, out var kill))
                {
                    allHumans.RemoveWhere(x => x.Owner == kill.Target);
                }
            }

            // no other humans to kill
            if (allHumans.Count == 0)
            {
                args.Cancelled = true;
                return;
            }

            _target.SetTarget(ent.Owner, _random.Pick(allHumans), target);
        }
    }
}
