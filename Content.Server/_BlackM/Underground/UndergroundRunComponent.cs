using Content.Shared._BlackM.Underground;

namespace Content.Server._BlackM.Underground;

[RegisterComponent]
public sealed partial class UndergroundRunComponent : Component
{
    public readonly UndergroundExpeditionSession Session = new();
    public EntityUid Boarding;
    public float HalfWidth;
    public UndergroundGeneratedMap? Site;
    public UndergroundCabin? Cabin;
    public EntityUid Landing;
    public HashSet<EntityUid> CabinFixtures = new();
    public TimeSpan NextUiUpdate;
    public TimeSpan? OutboundAt;
    public TimeSpan? OutboundAnnouncementAt;
    public EntityUid? BoardingDispatcher;
    public EntityUid? LandingDispatcher;
    public bool DepartureAnnounced;
    public UndergroundMission Mission = UndergroundMission.HeadcrabHunt;
    public int MissionProgress;
    public bool MissionRewardAvailable;
    public bool MissionClaimed;
    public bool MissionReturned;
    public bool MissionSampleAnnounced;
    public TimeSpan? FaultedAt;
    public int CompletedExpeditions;
    public int NextAmsRewardAt;

}

[RegisterComponent]
public sealed partial class UndergroundRideOwnerComponent : Component
{
    public EntityUid Console;
}
