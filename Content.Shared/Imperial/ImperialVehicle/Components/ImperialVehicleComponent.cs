using System.Numerics;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Imperial.ImperialVehicle.Components;

[AutoGenerateComponentState]
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedImperialVehicleSystem))]
public sealed partial class ImperialVehicleComponent : Component
{
    /// <summary>
    /// At this distance from the driver to the center of the vehicle, the driver unbuckle
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public float MaxRiderDistance = 0.1f;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public float BaseWalkSpeed = 2.0f;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public float BaseSprintSpeed = 3.0f;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public float Acceleration = 2.0f;

    [ViewVariables]
    public EntityUid? HornAction;

    /// <summary>
    /// What sound to play when the driver presses the horn action (plays once)
    /// </summary>
    [DataField]
    public SoundSpecifier? HornSound;

    /// <summary>
    /// The entity currently riding the vehicle.
    /// </summary>
    [ViewVariables]
    [AutoNetworkedField]
    public EntityUid? Rider;

    [ViewVariables]
    [AutoNetworkedField]
    public EntityUid? LastRider;

    /// <summary>
    /// The base offset for the vehicle (when facing east)
    /// </summary>
    [ViewVariables]
    public Vector2 BaseBuckleOffset = Vector2.Zero;

    /// <summary>
    /// Determines from which side the vehicle will be displayed on top of the player.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool SouthOver;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool NorthOver;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool WestOver;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool EastOver;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool AutoAnimate = true;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool UseHand = true;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool HideRider;
}
