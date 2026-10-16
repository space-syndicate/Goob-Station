using System.Diagnostics.CodeAnalysis;
using Content.Goobstation.Server._Aquila.Xenobiology.Components;
using Content.Goobstation.Shared._Aquila.Xenobiology;
using Content.Goobstation.Shared.Xenobiology;
using Content.Goobstation.Shared.Xenobiology.Components;
using Content.Server.Research.Systems;
using Content.Server.Station.Systems;
using Content.Shared.Popups;
using Content.Shared.Research.Components;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Server._Aquila.Xenobiology.Systems;

public sealed class SlimeDiscoverySystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly ResearchSystem _research = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StationSystem _station = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SlimeComponent, SlimeScannedEvent>(OnSlimeScanned);
    }

    private void OnSlimeScanned(Entity<SlimeComponent> ent, ref SlimeScannedEvent args)
    {
        var breed = ent.Comp.Breed;
        var points = GetDiscoveryPoints(breed);
        if (points <= 0 || _station.GetOwningStation(args.Scanner) is not { } station)
            return;

        var xenobiology = EnsureComp<StationXenobiologyComponent>(station);
        if (xenobiology.Discovered.Contains(breed)
            || !TryGetResearchServer(station, out var server))
            return;

        xenobiology.Discovered.Add(breed);
        _research.ModifyServerPoints(server.Value, points, server.Value.Comp);
        _popup.PopupEntity(Loc.GetString("slime-discovery-new-breed", ("points", points)), args.User, args.User, PopupType.Medium);
    }

    private int GetDiscoveryPoints(ProtoId<BreedPrototype> breed)
    {
        foreach (var group in _proto.EnumeratePrototypes<SlimeBreedGroupPrototype>())
        {
            if (group.Breeds.Contains(breed))
                return group.DiscoveryPoints;
        }

        return 0;
    }

    private bool TryGetResearchServer(EntityUid station, [NotNullWhen(true)] out Entity<ResearchServerComponent>? server)
    {
        var query = EntityQueryEnumerator<ResearchServerComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_station.GetOwningStation(uid) != station)
                continue;

            server = (uid, comp);
            return true;
        }

        server = null;
        return false;
    }
}
