using Content.Shared.Maps;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._Aquila.Heretic;

[RegisterComponent, Access(typeof(MansusRealmSystem))]
public sealed partial class MansusRealmComponent : Component
{
    [DataField]
    public int PlatformSize = 10;

    [DataField]
    public ProtoId<ContentTileDefinition> PlatformTile = "FloorDarkMarble";

    [DataField]
    public string Parallax = "AquilaMansus";

    [DataField]
    public Color AmbientLight = Color.FromHex("#9b86c4");

    [DataField]
    public List<string> CarriedSlots = new() { "jumpsuit", "shoes" };

    [DataField]
    public List<EntProtoId> HandPrototypes = new() { "MansusHand", "MansusHandRight" };

    [DataField]
    public float SpawnRadius = 12f;

    [DataField]
    public float MinHandSpeed = 3f;

    [DataField]
    public float MaxHandSpeed = 4.5f;

    [DataField]
    public TimeSpan GracePeriod = TimeSpan.FromSeconds(6);

    [DataField]
    public TimeSpan SurviveDuration = TimeSpan.FromSeconds(60);

    [DataField]
    public float StartInterval = 1.0f;

    [DataField]
    public float EndInterval = 0.32f;

    [DataField]
    public float HeavyIntervalMultiplier = 2f;

    [DataField]
    public float PatternUnlockTime = 8f;

    [DataField]
    public float WallUnlockTime = 20f;

    [DataField]
    public float FanWeight = 0.6f;

    [DataField]
    public float PincerWeight = 0.6f;

    [DataField]
    public float WallWeight = 0.4f;

    [DataField]
    public float FanCost = 2.2f;

    [DataField]
    public float PincerCost = 1.8f;

    [DataField]
    public float WallCost = 3.5f;

    [DataField]
    public float FanAngle = 14f;

    [DataField]
    public float WallSpacing = 0.8f;

    [DataField]
    public int WallGap = 3;

    [DataField]
    public float LockoutPadding = 0.3f;

    [DataField]
    public int RuneCount = 12;

    [DataField]
    public EntProtoId DormantRune = "MansusRuneDormant";

    [DataField]
    public EntProtoId LitRune = "MansusRuneLit";

    [DataField]
    public SoundSpecifier Music = new SoundPathSpecifier("/Audio/_Goobstation/Heretic/Ambience/Antag/Heretic/VoidsEmbrace.ogg", AudioParams.Default.WithLoop(true));

    [DataField]
    public float MusicFadeVolume = -30f;

    [DataField]
    public TimeSpan LeaveDuration = TimeSpan.FromSeconds(3);

    [DataField]
    public TimeSpan SleepDuration = TimeSpan.FromSeconds(30);

    [DataField]
    public EntProtoId ReturnEffect = "EffectVoidBlinkIn";

    [DataField]
    public SoundSpecifier ReturnSound = new SoundPathSpecifier("/Audio/_Goobstation/Heretic/voidblink.ogg");

    [DataField]
    public EntityUid Victim;

    [DataField]
    public EntityUid Mind;

    [DataField]
    public EntityUid Grid;

    [DataField]
    public EntityUid? ReturnStation;

    [DataField]
    public EntityUid? MusicStream;

    [DataField]
    public TimeSpan StartTime;

    [DataField]
    public TimeSpan NextHand;

    [DataField]
    public TimeSpan NextHeavy;

    [DataField]
    public TimeSpan? LeaveTime;

    [DataField]
    public List<EntityUid> Runes = new();

    [DataField]
    public int LitRunes;

    [DataField]
    public bool Finished;
}
