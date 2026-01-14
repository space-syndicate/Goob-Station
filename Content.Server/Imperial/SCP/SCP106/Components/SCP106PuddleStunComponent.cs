using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
namespace Content.Server.Imperial.SCP.SCP106.Components;

[RegisterComponent]
public sealed partial class SCP106PuddleStunComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan Delay { get; set; } = TimeSpan.FromSeconds(3);
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan StunEnd = TimeSpan.Zero;
    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid Puddle = EntityUid.Invalid;
}
