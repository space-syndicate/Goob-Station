using Content.Shared.Guidebook;
using Robust.Shared.GameStates;
using Content.Shared.Atmos;

namespace Content.Shared.Imperial.Atmos.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class HydrogenGasPressurePumpComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    [DataField("inlet")]
    public string InletName = "inlet";

    [DataField("outlet")]
    public string OutletName = "outlet";

    [DataField, AutoNetworkedField]
    public float TargetPressure = Atmospherics.OneAtmosphere;

    /// <summary>
    ///     Max pressure of the target gas (NOT relative to source).
    /// </summary>
    [DataField]
    [GuidebookData]
    public float MaxTargetPressure = Atmospherics.HydrogenMaxOutputPressure;
}
