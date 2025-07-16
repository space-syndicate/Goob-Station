using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Imperial.Medieval.Administration.Nrp;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NrpCurseComponent : Component
{
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadOnly)]
    public ProtoId<NrpIconPrototype> Icon = "NRPIcon";
}
