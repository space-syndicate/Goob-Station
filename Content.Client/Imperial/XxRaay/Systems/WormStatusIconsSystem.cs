using Content.Shared.Imperial.XxRaay.Components;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.Imperial.XxRaay.Systems;

public sealed class WormStatusIconsSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private static readonly ProtoId<FactionIconPrototype> WormFactionIcon = "WormFaction";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WormBloodComponent, GetStatusIconsEvent>(OnGetWormStatusIcon);
        SubscribeLocalEvent<WormCorpseOccupiedComponent, GetStatusIconsEvent>(OnGetWormStatusIcon);
        SubscribeLocalEvent<WormDoorHideOccupiedComponent, GetStatusIconsEvent>(OnGetWormStatusIcon);
    }

    private void OnGetWormStatusIcon<T>(Entity<T> ent, ref GetStatusIconsEvent args)
        where T : Component
    {
        if (!_prototype.TryIndex(WormFactionIcon, out var iconPrototype))
            return;

        args.StatusIcons.Add(iconPrototype);
    }
}
