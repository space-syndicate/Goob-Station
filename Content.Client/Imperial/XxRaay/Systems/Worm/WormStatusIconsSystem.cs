using Content.Shared.Imperial.XxRaay.Components;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;
namespace Content.Client.Imperial.XxRaay.Systems;

public sealed class WormStatusIconsSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WormBloodComponent, GetStatusIconsEvent>(OnGetWormStatusIcon);
        SubscribeLocalEvent<WormCorpseOccupiedComponent, GetStatusIconsEvent>(OnGetWormCorpseStatusIcon);
    }

    private void OnGetWormStatusIcon(Entity<WormBloodComponent> ent, ref GetStatusIconsEvent args)
    {
        if (HasComp<ActiveWormDoorHidingComponent>(ent.Owner))
            return;

        if (!_prototype.TryIndex(ent.Comp.FactionIcon, out var iconPrototype))
            return;

        args.StatusIcons.Add(iconPrototype);
    }

    private void OnGetWormCorpseStatusIcon(Entity<WormCorpseOccupiedComponent> ent, ref GetStatusIconsEvent args)
    {
        if (HasComp<ActiveWormDoorHidingComponent>(ent.Owner))
            return;

        if (!_prototype.TryIndex(ent.Comp.FactionIcon, out var iconPrototype))
            return;

        args.StatusIcons.Add(iconPrototype);
    }
}
