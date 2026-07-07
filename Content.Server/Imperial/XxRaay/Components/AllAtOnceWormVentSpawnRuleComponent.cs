using Content.Server.Imperial.XxRaay.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.Imperial.XxRaay.Components;

[RegisterComponent, Access(typeof(AllAtOnceWormVentSpawnRule))]
public sealed partial class AllAtOnceWormVentSpawnRuleComponent : Component
{
    [DataField]
    public EntProtoId Prototype = "MobWormTier1";

    [DataField]
    public int Count = 6;

    [DataField]
    public LocId? StartAnnouncement = "station-event-worm-vent-spawn-start-announcement";

    [DataField]
    public Color StartAnnouncementColor = Color.Gold;

    [DataField]
    public SoundSpecifier? StartAudio = new SoundPathSpecifier("/Audio/Announcements/aliens.ogg");
}
