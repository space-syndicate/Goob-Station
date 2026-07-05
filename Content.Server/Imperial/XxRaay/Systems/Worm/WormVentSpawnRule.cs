using Content.Server.Imperial.XxRaay.Components;
using Content.Server.StationEvents.Components;
using Content.Server.StationEvents.Events;
using Content.Shared.GameTicking.Components;
using Content.Shared.Station.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Imperial.XxRaay.Systems;

public sealed class WormVentSpawnRule : StationEventSystem<WormVentSpawnRuleComponent>
{
    protected override void Started(EntityUid uid, WormVentSpawnRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (!TrySpawnWormsOnRandomStationVents(component.Prototype, component.Count, args.RuleId))
            ForceEndSelf(uid, gameRule);
    }

    public bool TrySpawnWormsOnRandomStationVents(EntProtoId prototype, int count, string ruleId)
    {
        if (!TryGetRandomStation(out var station))
            return false;

        var vents = GetStationVents(station.Value);
        if (vents.Count == 0)
        {
            Sawmill.Warning($"Unable to find a valid vent for {ruleId}!");
            return false;
        }

        RobustRandom.Shuffle(vents);

        var spawnCount = Math.Min(count, vents.Count);
        for (var i = 0; i < spawnCount; i++)
        {
            var coords = Transform(vents[i]).Coordinates;
            Sawmill.Info($"Spawning {prototype} at {coords}");
            Spawn(prototype, coords);
        }

        return true;
    }

    private List<EntityUid> GetStationVents(EntityUid station)
    {
        var vents = new List<EntityUid>();
        var query = EntityQueryEnumerator<VentCritterSpawnLocationComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out _, out var transform))
        {
            if (!transform.Anchored)
                continue;

            if (CompOrNull<StationMemberComponent>(transform.GridUid)?.Station != station)
                continue;

            vents.Add(uid);
        }

        return vents;
    }
}
