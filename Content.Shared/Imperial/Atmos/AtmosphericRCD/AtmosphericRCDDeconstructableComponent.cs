using Content.Shared.Imperial.Atmospheric.RCD.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations;

namespace Content.Shared.Imperial.Atmospheric.RCD.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(AtmosphericRCDSystem))]
public sealed partial class AtmosphericRCDDeconstructableComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int Cost = 1;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan Delay = TimeSpan.FromSeconds(1);

    [DataField("fx"), ViewVariables(VVAccess.ReadWrite)]
    public EntProtoId? Effect = null;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool Deconstructable = true;
}
