using Content.Shared.Imperial.Medieval.Administration.Nrp;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.Imperial.Medieval.Administration.UI.Nrp;

public sealed class NrpCurseSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NrpCurseComponent, GetStatusIconsEvent>(OnGetStatusIcon);
    }

    private void OnGetStatusIcon(EntityUid uid, NrpCurseComponent component, ref GetStatusIconsEvent args)
    {
        if (!_mobState.IsDead(uid) &&
            !HasComp<ActiveNPCComponent>(uid) &&
            TryComp<MindContainerComponent>(uid, out var mindContainer) &&
            HasComp<MindExaminableComponent>(uid))
        {
            args.StatusIcons.Add(_prototype.Index(component.Icon));
        }
    }
}
