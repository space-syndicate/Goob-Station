using Robust.Shared.GameStates;
using Robust.Shared.Timing;

namespace Content.Shared.Imperial.XxRaay.Zero.Overthrow;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class OverthrowComponent : Component
{
    [DataField, AutoNetworkedField]
    public float ThrowMultiplier = 2.0f;

    [DataField, AutoNetworkedField]
    public float Cooldown = 10f;

    [DataField, AutoNetworkedField]
    public TimeSpan? LastThrowTime;

    [DataField, AutoNetworkedField]
    public int Damage = 20;

    [DataField, AutoNetworkedField]
    public float KnockdownTime = 2f;
}
