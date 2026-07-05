using Content.Shared.Alert;
using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Imperial.XxRaay.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class WormBloodComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Blood;

    [DataField, AutoNetworkedField]
    public int MaxBlood = 1000;

    [DataField]
    public ProtoId<AlertPrototype> BloodAlert = "WormBlood";

    [DataField]
    public ProtoId<FactionIconPrototype> FactionIcon = "WormFaction";

    [ViewVariables]
    public short LastAlertSeverity = -1;
}
