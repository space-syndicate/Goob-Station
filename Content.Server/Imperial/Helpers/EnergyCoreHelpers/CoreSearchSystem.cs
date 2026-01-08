using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Content.Shared.Imperial.EnergyCore.Components;
using Content.Server.Imperial.EnergyCore.Components;

namespace Content.Server.Imperial.EnergyCore.Helpers;
public sealed class CoreSearchSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    public EntityUid? FindNearestEnergyCore(EntityUid core, float distantion)
    {
        var transformCompConsole = Transform(core);
        var mapId = transformCompConsole.MapID;
        var pos = _transformSystem.GetMapCoordinates(transformCompConsole).Position;

        EntityUid? nearest = null;
        var minDist = distantion;

        var enumerator = EntityQueryEnumerator<EnergyCoreComponent, TransformComponent>();
        while (enumerator.MoveNext(out var uid, out _, out var transComp))
        {
            if (transComp.MapID != mapId)
                continue;

            var corepos = _transformSystem.GetMapCoordinates(uid).Position;
            var dist = (corepos - pos).LengthSquared();
            if (dist > minDist)
                continue;

            minDist = dist;
            nearest = uid;
        }
        return nearest;
    }
    public EntityUid? FindNearestProtocolTerminal(EntityUid terminal, float distantion)
    {
        var transformCompConsole = Transform(terminal);
        var mapId = transformCompConsole.MapID;
        var pos = _transformSystem.GetMapCoordinates(transformCompConsole).Position;

        EntityUid? nearest = null;
        var minDist = distantion;

        var enumerator = EntityQueryEnumerator<CoreAccessComputerComponent, TransformComponent>();
        while (enumerator.MoveNext(out var uid, out _, out var transComp))
        {
            if (transComp.MapID != mapId)
                continue;

            var corepos = _transformSystem.GetMapCoordinates(uid).Position;
            var dist = (corepos - pos).LengthSquared();
            if (dist > minDist)
                continue;

            minDist = dist;
            nearest = uid;
        }
        return nearest;
    }
}
