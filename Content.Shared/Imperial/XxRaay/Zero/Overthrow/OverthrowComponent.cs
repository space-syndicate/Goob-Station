using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Imperial.XxRaay.Zero.Overthrow;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class OverthrowComponent : Component
{
    [DataField, AutoNetworkedField]
    public float ThrowMultiplier = 2.0f;

    [DataField, AutoNetworkedField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(10);

    [AutoNetworkedField, ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan LastThrowTime = TimeSpan.Zero;

    [DataField, AutoNetworkedField]
    public DamageSpecifier Damage = new();

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    public TimeSpan KnockdownTime = TimeSpan.FromSeconds(2);

    [DataField, AutoNetworkedField]
    public string CooldownAlertId = "OverthrowCooldown";
}
