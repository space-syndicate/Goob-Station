using Robust.Shared.GameStates;

namespace Content.Shared.Imperial.MiningWeapons.Smasher.Components;


[AutoGenerateComponentState]
[RegisterComponent, NetworkedComponent]
public sealed partial class SmasherChargingComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float WalkSpeedModifier = 0.5f;

    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float SprintSpeedModifier = 0.3f;
}
