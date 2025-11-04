using Robust.Shared.GameStates;

namespace Content.Shared.Imperial.ImperialVehicle.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ImperialVehiclePilotComponent : Component
{
    /// <summary>
    /// The vehicle this rider is currently riding.
    /// </summary>
    [ViewVariables] public EntityUid? Vehicle;

    public override bool SendOnlyToOwner => true;
}
