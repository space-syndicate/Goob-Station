using Robust.Shared.Map;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
namespace Content.Server.Imperial.SCP.SCP106.Components;

[RegisterComponent]
public sealed partial class SCP106SkullComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(3);
    [DataField("puddleExitSound")]
    public SoundSpecifier ExitSound = new SoundPathSpecifier("/Audio/Imperial/SCP/scp106_puddle_exit.ogg");
}