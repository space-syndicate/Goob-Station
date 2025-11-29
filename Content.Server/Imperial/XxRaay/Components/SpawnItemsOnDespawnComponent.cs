using Content.Server.Imperial.XxRaay.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server.Imperial.XxRaay.Components;

/// <summary>
/// When a <c>TimedDespawnComponent</c> despawns, spawns multiple items from the list.
/// </summary>
[RegisterComponent, Access(typeof(SpawnItemsOnDespawnSystem))]
public sealed partial class SpawnItemsOnDespawnComponent : Component
{
    /// <summary>
    /// List of entity prototypes to spawn when the entity despawns.
    /// </summary>
    [DataField]
    public List<EntProtoId> Items = new();
}

