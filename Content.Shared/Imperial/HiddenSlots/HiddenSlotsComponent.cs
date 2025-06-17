using Content.Shared.Dataset;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Imperial.HiddenSlots;

/// <summary>
///     Component that defines a set of equipment or inventory slots
///     which are blocked or hidden for the entity.
/// </summary>
[RegisterComponent]
[NetworkedComponent]
public sealed partial class HiddenSlotsComponent : Component
{
    /// <summary>
    ///     Reference to a dataset prototype containing the list of slots
    ///     that should be blocked or hidden on this entity.
    /// </summary>
    [DataField]
    public ProtoId<DatasetPrototype> BlockedSlots;
}
