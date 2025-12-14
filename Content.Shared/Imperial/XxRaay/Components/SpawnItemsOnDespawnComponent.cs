using Robust.Shared.Prototypes;

namespace Content.Shared.Imperial.XxRaay.Components;

/// <summary>
/// Holds a list of items to spawn when the entity despawns.
/// </summary>
[RegisterComponent]
public sealed partial class SpawnItemsOnDespawnComponent : Component
{
    /// <summary>
    /// List of entity prototypes to spawn on despawn.
    /// </summary>
    [DataField]
    public List<EntProtoId> Items = new();
}

