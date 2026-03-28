using Content.Server.Imperial.Atmos.Piping.Trinary.EntitySystems;
using Content.Shared.Atmos;

namespace Content.Server.Imperial.Atmos.Piping.Trinary.Components
{
    [RegisterComponent]
    [Access(typeof(HydrogenGasMixerSystem))]
    public sealed partial class HydrogenGasMixerComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField]
        public bool Enabled = true;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("inletOne")]
        public string InletOneName = "inletOne";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("inletTwo")]
        public string InletTwoName = "inletTwo";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("outlet")]
        public string OutletName = "outlet";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField]
        public float TargetPressure = Atmospherics.OneAtmosphere;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField]
        public float MaxTargetPressure = Atmospherics.HydrogenMaxOutputPressure;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField]
        public float InletOneConcentration = 0.5f;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField]
        public float InletTwoConcentration = 0.5f;
    }
}
