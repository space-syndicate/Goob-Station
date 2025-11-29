using Content.Server.Imperial.XxRaay.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Spawners;

namespace Content.Server.Imperial.XxRaay.Systems;

/// <summary>
/// System that handles spawning multiple items when an entity with SpawnItemsOnDespawnComponent despawns.
/// </summary>
public sealed class SpawnItemsOnDespawnSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpawnItemsOnDespawnComponent, TimedDespawnEvent>(OnDespawn);
    }

    private void OnDespawn(EntityUid uid, SpawnItemsOnDespawnComponent comp, ref TimedDespawnEvent args)
    {
        if (!TryComp(uid, out TransformComponent? xform))
            return;

        foreach (var itemProto in comp.Items)
        {
            Spawn(itemProto, xform.Coordinates);
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

