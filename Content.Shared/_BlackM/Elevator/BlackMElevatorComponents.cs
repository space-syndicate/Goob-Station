using Content.Shared.Roles;
using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Content.Shared._BlackM.Elevator;

[RegisterComponent]
public sealed partial class BlackMElevatorStationComponent : Component
{
    [DataField(required: true)]
    public ResPath ElevatorMap;

    [DataField]
    public HashSet<ProtoId<JobPrototype>> BlacklistedJobs = new();

    [DataField] public string SpawnElevatorId = "main";

    [ViewVariables] public MapId? ElevatorMapId;
    [ViewVariables] public EntityUid? Elevator;
    [ViewVariables] public bool LoadFailed;
}

[DataDefinition]
public sealed partial class BlackMElevatorAnnouncement
{
    [DataField(required: true)] public float At;
    [DataField(required: true)] public LocId Message;
    [DataField] public bool Big;
}

[RegisterComponent]
public sealed partial class BlackMElevatorComponent : Component
{
    [DataField] public string Id = "main";

    [DataField] public bool Evacuation;

    [DataField] public bool Enabled;

    [DataField] public bool GlobalAnnounce = true;

    [DataField] public string? PointId;

    [DataField] public string? LocationName;

    [DataField] public LocId Sender = "blackm-elevator-evac-sender";
    [DataField] public LocId BoardingMessage = "blackm-elevator-evac-boarding";
    [DataField] public LocId ClosingMessage = "blackm-elevator-evac-closing";
    [DataField] public LocId DepartingMessage = "blackm-elevator-evac-departing";
    [DataField] public LocId CountdownStartMessage = "blackm-elevator-countdown-start";
    [DataField] public LocId CancelMessage = "blackm-elevator-cancel";
    [DataField] public LocId FadeText = "blackm-elevator-fade-text";

    [DataField] public string DestinationId = "default";

    [DataField] public float BoardingDelay = 12f;
    [DataField] public float DescentTime = 30f;
    [DataField] public float CooldownTime = 15f;
    [DataField] public float JoinCutoff = 8f;
    [DataField] public float FadeOutLead = 2.5f;
    [DataField] public float FadeInTime = 2.5f;
    [DataField] public float EmergencyLightsAt = 22f;

    [DataField] public float ClosingWarningAt = 10f;

    [DataField] public int PopupCountdownFrom = 5;

    [DataField] public float DepartFadeTime = 1.2f;

    [DataField] public float SmallShakeMin = 0.12f;
    [DataField] public float SmallShakeMax = 0.28f;
    [DataField] public float HeavyShakeStrength = 0.9f;
    [DataField] public List<float> HeavyShakes = new() { 15f, 24f };

    [DataField] public float RumbleIntensity = 1f;

    [DataField] public float? BrakeAt;

    [DataField] public float BrakeStrength = 0.6f;
    [DataField] public float LandingStrength = 0.45f;

    [DataField] public List<BlackMElevatorAnnouncement> Announcements = new();

    [DataField] public SoundSpecifier? StartSound;

    [ViewVariables] public BlackMElevatorState State = BlackMElevatorState.Idle;
    [ViewVariables] public BlackMElevatorLightMode LightMode = BlackMElevatorLightMode.Normal;
    public TimeSpan StateStart;
    public TimeSpan BoardingEnd;
    public TimeSpan NextShake;
    public TimeSpan? LightsRestoreAt;
    public int AnnounceIndex;
    public int HeavyIndex;
    public bool BoardingWarned;
    public bool CountdownActive;
    public int LastCountdownSecond = int.MaxValue;
    public TimeSpan NextScan;
    public readonly HashSet<EntityUid> InZone = new();
    public readonly HashSet<EntityUid> Faded = new();
    public bool FadedOut;
    public bool Braked;
    public TimeSpan NextRideSync;
    public readonly HashSet<EntityUid> Passengers = new();
    public readonly HashSet<EntityUid> Waiting = new();
}

[RegisterComponent]
public sealed partial class BlackMElevatorSpawnPointComponent : Component
{
    [DataField] public string? PointId;
}

[RegisterComponent]
public sealed partial class BlackMElevatorDestinationComponent : Component
{
    [DataField] public string Id = "default";
}

[RegisterComponent]
public sealed partial class BlackMElevatorLightComponent : Component
{
    [ViewVariables] public float BaseEnergy = 1f;
    [ViewVariables] public Color BaseColor = Color.White;
}

[RegisterComponent]
public sealed partial class BlackMElevatorDispatcherComponent : Component
{
    [DataField] public string? ElevatorId;

    [DataField] public string? PointId;
}

[RegisterComponent]
public sealed partial class BlackMElevatorBoardZoneComponent : Component
{
    [DataField] public string ElevatorId = "evac";
    [DataField] public float Range = 2.5f;
    [DataField] public string? PointId;
    [DataField] public List<ProtoId<TagPrototype>> RestrictedTags = new();
}