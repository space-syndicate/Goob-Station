using Robust.Shared.GameStates;

namespace Content.Shared.Imperial.XxRaay.Zero.Overthrow;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class OverthrownItemComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Damage = 20;

    [DataField, AutoNetworkedField]
    public float KnockdownTime = 2f;
    
    [DataField, AutoNetworkedField]
    public bool IsOverthrown = true;
}
