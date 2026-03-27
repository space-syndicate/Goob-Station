using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Imperial.XxRaay.Components;

/// <summary>
/// Tracks custom energy level and exposes an alert for it.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AlertEnergyComponent : Component
{
    /// <summary>
    /// Current accumulated energy.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Energy = 0f;

    /// <summary>
    /// Maximum energy allowed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MaxEnergy = 50f;

    /// <summary>
    /// Energy per alert severity step.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Step = 5f;

    /// <summary>
    /// Passive energy regeneration per second.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float RegenPerSecond = 0.5f;

    /// <summary>
    /// Alert prototype used for displaying the energy value.
    /// </summary>
    [DataField]
    public ProtoId<AlertPrototype> AlertId = "energy_alert";
}

