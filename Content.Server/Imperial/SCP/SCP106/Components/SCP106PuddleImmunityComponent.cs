using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
namespace Content.Server.Imperial.SCP.SCP106.Components;

[RegisterComponent]
public sealed partial class SCP106PuddleImmunityComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan ImmunityEnd = TimeSpan.Zero;
}
