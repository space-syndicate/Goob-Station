using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._BlackM.Announcement;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class AnnouncementConsoleComponent : Component
{
    [DataField]
    public List<ProtoId<AnnouncementChannelPrototype>> Channels = new();

    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(30);

    [DataField]
    public int MaxLength = 256;

    [DataField]
    public bool IgnoreChannelAccess;

    [DataField]
    public float Duration;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextAnnouncement;
}
