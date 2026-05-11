using Content.Shared.Atmos;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Imperial.Power.Components;

/// <summary>
/// Configuration and runtime state for supermatter gas effects.
/// </summary>
[RegisterComponent]
public sealed partial class SupermatterGasComponent : Component
{
    /// <summary>
    /// Threshold moles of a gas in the chamber for the effect to be considered active.
    /// </summary>
    [DataField]
    public float GasActivationMoles = 15f;

    /// <summary>
    /// How many moles of an active gas are consumed per second.
    /// </summary>
    [DataField]
    public float GasConsumptionPerSecond = 3f;

    /// <summary>
    /// Whether Anti-Noblium hard shutdown is enabled.
    /// </summary>
    [DataField]
    public bool AntiNobliumHardShutdownEnabled = true;

    [ViewVariables(VVAccess.ReadWrite)]
    public float RuntimeRadiationMultiplier = 1f;

    [ViewVariables(VVAccess.ReadWrite)]
    public float RuntimeLightningMultiplier = 1f;

    [ViewVariables(VVAccess.ReadWrite)]
    public float RuntimeEventSpeedMultiplier = 1f;

    [ViewVariables(VVAccess.ReadWrite)]
    public bool RuntimeDisableTouchGib;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan LastAtmosUpdate;

    public bool WasShutdownByAntiNoblium;

    [ViewVariables(VVAccess.ReadOnly)]
    public GasMixture? CachedGasMixture;
}

