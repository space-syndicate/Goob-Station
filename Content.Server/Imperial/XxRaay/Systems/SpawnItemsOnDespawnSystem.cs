using Content.Shared.Imperial.XxRaay.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server.Imperial.XxRaay.Systems;

/// <summary>
/// System that handles spawning multiple items when an entity with SpawnItemsOnDespawnComponent despawns.
/// </summary>
public sealed class SpawnItemsOnDespawnSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpawnItemsOnDespawnComponent, EntityTerminatingEvent>(OnDespawn);
    }

    private void OnDespawn(EntityUid uid, SpawnItemsOnDespawnComponent comp, ref EntityTerminatingEvent args)
    {
        if (comp.Items.Count == 0)
            return;

        if (!TryComp<TransformComponent>(uid, out var xform))
            return;

        var coords = xform.Coordinates;

        foreach (var itemProto in comp.Items)
        {
            Spawn(itemProto, coords);
        }
    }

    /// <summary>
    /// Sets the list of items to spawn when the entity despawns.
    /// </summary>
    public void SetItems(Entity<SpawnItemsOnDespawnComponent> entity, List<EntProtoId> items)
    {
        entity.Comp.Items.Clear();
        entity.Comp.Items.AddRange(items);
    }
}

