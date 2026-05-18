using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

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
}
