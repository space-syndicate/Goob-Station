using Robust.Shared.Map;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Content.Shared.Damage;
namespace Content.Server.Imperial.SCP.SCP106.Components;

[RegisterComponent]
public sealed partial class SCP106DimensionDebuffComponent : Component
{    [DataField(required: true), ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier DamagePerSecond = new();
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextDamage = TimeSpan.Zero;
}