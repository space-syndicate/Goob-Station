using Content.Server.Explosion.EntitySystems;
using Content.Shared.Trigger;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.Imperial.Medieval.Magic.Triggers;


public sealed partial class SpawnEntityOnTriggerSystem : EntitySystem
{
    [Dependency] private readonly TransformSystem _transformSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpawnEntityOnTriggerComponent, TriggerEvent>(OnTrigger);
    }

    private void OnTrigger(EntityUid uid, SpawnEntityOnTriggerComponent component, TriggerEvent args)
    {
        var spawnCoord = _transformSystem.GetMapCoordinates(uid);

        foreach (var entProtoId in component.SpawnedEntitiesPrototype)
        {
            var data = _prototypeManager.Index(entProtoId);
            var offsetCoords = new MapCoordinates(spawnCoord.Position + data.PlacementOffset, spawnCoord.MapId);

            Spawn(entProtoId, offsetCoords);
        }
    }
}
