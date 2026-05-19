using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Audio; // Imperial Space "plasma Cutter + Advanced Version"

namespace Content.Shared.Weapons.Ranged.Components;

/// <summary>
/// Allows battery weapons to fire different types of projectiles
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(BatteryWeaponFireModesSystem))]
[AutoGenerateComponentState]
public sealed partial class BatteryWeaponFireModesComponent : Component
{
    /// <summary>
    /// A list of the different firing modes the weapon can switch between
    /// </summary>
    [DataField(required: true)]
    [AutoNetworkedField]
    public List<BatteryWeaponFireMode> FireModes = new();

    /// <summary>
    /// The currently selected firing mode
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public int CurrentFireMode;

    // Imperial Space "plasma Cutter + Advanced Version" Start
    /// <summary>
    /// Delay between switching shooting modes
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public TimeSpan ModeSwitchDelay = TimeSpan.FromSeconds(2);

    /// <summary>
    /// When can I switch again
    /// </summary>
    [AutoNetworkedField]
    public TimeSpan NextModeSwitchTime = TimeSpan.Zero;

    /// <summary>
    /// The standard sound when switching shooting modes
    /// </summary>
    [DataField]
    public SoundSpecifier ModeSwitchSound = new SoundPathSpecifier("/Audio/Imperial/click-switch-gun-mode.ogg");
    // Imperial Space "plasma Cutter + Advanced Version" End
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class BatteryWeaponFireMode
{
    /// <summary>
    /// The projectile prototype associated with this firing mode
    /// </summary>
    [DataField("proto", required: true)]
    public EntProtoId Prototype = default!;

    /// <summary>
    /// The battery cost to fire the projectile associated with this firing mode
    /// </summary>
    [DataField]
    public float FireCost = 100;

    /// <summary>
    /// Wether or not this fire mode can be used by pacifists
    /// </summary>
    [DataField]
    public bool PacifismAllowedMode = false;
}

[Serializable, NetSerializable]
public enum BatteryWeaponFireModeVisuals : byte
{
    State
}
