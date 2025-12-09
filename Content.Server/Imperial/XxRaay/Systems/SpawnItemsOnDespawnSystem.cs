using Content.Shared.Imperial.XxRaay.Components;
using Content.Shared.Destructible;
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

        SubscribeLocalEvent<SpawnItemsOnDespawnComponent, DestructionEventArgs>(OnDespawn);
    }

    private void OnDespawn(EntityUid uid, SpawnItemsOnDespawnComponent comp, ref DestructionEventArgs args)
    {
        var xform = Transform(uid);

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

