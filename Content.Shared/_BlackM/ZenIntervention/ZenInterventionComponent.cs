using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._BlackM.ZenIntervention;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ZenInterventionComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Level;

    [DataField, AutoNetworkedField]
    public float MaxLevel = 100f;

    [DataField, AutoNetworkedField]
    public float GainPerMinute = 1f;

    [DataField, AutoNetworkedField]
    public float BreachThreshold = 60f;

    [DataField, AutoNetworkedField]
    public List<float> IntermediateBreachThresholds = new() { 40f };

    [DataField, AutoNetworkedField]
    public int IntermediateBreachSpawnAmount = 4;

    [ViewVariables]
    public readonly HashSet<float> TriggeredIntermediateBreaches = new();

    [DataField, AutoNetworkedField]
    public bool BreachTriggered;

    [DataField, AutoNetworkedField]
    public List<EntProtoId> BreachMobPrototypes = new()
    {
        "MobHeadcrabStationBlackM",
        "MobHoundeyeStationBlackM",
        "MobVortigauntStationBlackM",
    };

    [DataField, AutoNetworkedField]
    public int BreachSpawnAmount = 8;

    [DataField, AutoNetworkedField]
    public bool WaveActive;

    [DataField, AutoNetworkedField]
    public TimeSpan NextWaveTime = TimeSpan.Zero;

    [DataField, AutoNetworkedField]
    public TimeSpan WaveInterval = TimeSpan.FromMinutes(5);

    [DataField, AutoNetworkedField]
    public int WaveSpawnAmount = 4;

    [DataField, AutoNetworkedField]
    public EntProtoId WaveMobPrototype = "MobXenInfected";

    [DataField]
    public int MaxPlacementAttempts = 20;

    [DataField]
    public SoundSpecifier BreachSound = new SoundPathSpecifier("/Audio/_BlackM/Announcements/60.ogg");

    [DataField]
    public SoundSpecifier WaveSound = new SoundPathSpecifier("/Audio/_BlackM/Announcements/100.ogg");

    [DataField]
    public Color? AnnouncementColor;

    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    [DataField]
    public string TargetStationName = "Комплекс Черная Меза";
}
