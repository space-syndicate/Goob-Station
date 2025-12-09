using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Imperial.XxRaay.Zero.Overthrow;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class OverthrownItemComponent : Component
{
    [DataField, AutoNetworkedField]
    public DamageSpecifier Damage = new();

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    public TimeSpan KnockdownTime = TimeSpan.Zero;
    
    [DataField, AutoNetworkedField]
    public bool IsOverthrown = true;
}
