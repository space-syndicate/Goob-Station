using Content.Shared.StatusIcon.Components;
using Content.Shared.Imperial.PiratesNewHorizon.StatusIcons;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;
using Content.Shared.Imperial.Vampire;

namespace Content.Client.Imperial.Vampire.StatusIcons;

public sealed class VampireStatusIconsSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VampireComponent, GetStatusIconsEvent>(GetVampireIcon);
        SubscribeLocalEvent<GhoulComponent, GetStatusIconsEvent>(GetGhoulIcon);
    }

    private void GetVampireIcon(Entity<VampireComponent> ent, ref GetStatusIconsEvent args)
    {
        if (!_prototype.TryIndex(ent.Comp.StatusIcon, out var iconPrototype))
            return;

        args.StatusIcons.Add(iconPrototype);
    }

    private void GetGhoulIcon(Entity<GhoulComponent> ent, ref GetStatusIconsEvent args)
    {
        if (!_prototype.TryIndex(ent.Comp.StatusIcon, out var iconPrototype))
            return;

        args.StatusIcons.Add(iconPrototype);
    }
}
