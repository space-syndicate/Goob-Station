using Robust.Shared.GameStates;

namespace Content.Shared.Traits.Assorted;

/// <summary>
/// Set player speed to zero and standing state to down, simulating leg paralysis.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(LegsParalyzedSystem))]
public sealed partial class LegsParalyzedComponent : Component
{
    [DataField] public float CrawlMoveSpeed = 0; // Imperial "ImperialVehicle"
    [DataField] public float CrawlMoveAcceleration = 0; // Imperial "ImperialVehicle"
}
