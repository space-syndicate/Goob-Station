using Robust.Shared.GameStates;

namespace Content.Shared.Imperial.Lavaland.MiningWeapons.Smasher.Components;


[RegisterComponent, NetworkedComponent]
public sealed partial class SmasherChargingComponent : Component
{
    [DataField("walkSpeedModifier")]
    public float WalkSpeedModifier = 0.5f;

    [DataField("sprintSpeedModifier")]
    public float SprintSpeedModifier = 0.3f;
}
