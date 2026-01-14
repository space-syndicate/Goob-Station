using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Content.Shared.Damage;
using Robust.Shared.Map;
namespace Content.Server.Imperial.SCP.SCP106.Components;

[RegisterComponent]
public sealed partial class SCP106DimensionDebuffComponent : Component
{
    [DataField(required: true), ViewVariables]
    public DamageSpecifier DamagePerSecond = new();
    [ViewVariables]
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextDamage = TimeSpan.Zero;
    [ViewVariables]
    public MapId? PastMapId;
}
