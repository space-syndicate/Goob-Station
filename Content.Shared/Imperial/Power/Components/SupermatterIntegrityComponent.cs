using Content.Shared.Damage;
using Content.Shared.Radio;
using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Imperial.Power.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(fieldDeltas: true), AutoGenerateComponentPause]
public sealed partial class SupermatterIntegrityComponent : Component
{
    [DataField]
    public List<SupermatterIntegrityLevel> SupermatterIntegrity = new()
    {
        new() { Threshold = 95f, Color = Color.Green, Description = "supermatter-desc-pristine", Warning = "supermatter-warn-95" },
        new() { Threshold = 75f, Color = Color.Yellow, Description = "supermatter-desc-scratched", Warning = "supermatter-warn-75" },
        new() { Threshold = 50f, Color = Color.Orange, Description = "supermatter-desc-cracked", Warning = "supermatter-warn-50" },
        new() { Threshold = 25f, Color = Color.Brown, Description = "supermatter-desc-badly-cracked", Warning = "supermatter-warn-25" },
        new() { Threshold = 10f, Color = Color.DarkRed, Description = "supermatter-desc-critical", Warning = "supermatter-warn-10" },
        new() { Threshold = 0f, Color = Color.Red, Description = "", Warning = "" },
    };

    [DataField, AutoNetworkedField]
    public bool Activated;

    [ViewVariables(VVAccess.ReadWrite)]
    public bool CatastropheActivated;


    [DataField, AutoNetworkedField]
    public float Integrity = 100f;

    [DataField, AutoNetworkedField]
    public float MaxIntegrity = 100f;


    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public DamageSpecifier TickDamage = new();

    [DataField]
    public TimeSpan DamageInterval = TimeSpan.FromSeconds(1);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextDamageTime;


    [DataField]
    public float CatastropheThreshold;

    [ViewVariables(VVAccess.ReadWrite)]
    [AutoPausedField]
    public TimeSpan CatastropheEndTime = TimeSpan.Zero;

    [DataField]
    public TimeSpan CatastropheDuration = TimeSpan.FromSeconds(120);


    [DataField]
    public (float, float) TempThresholds = new(250f, 350f);

    [DataField]
    public (float, float) PressureThresholds = new(10f, 300f);


    [DataField]
    public ProtoId<RadioChannelPrototype>[] RadioChannels = ["Engineering"];


    [DataField]
    public ProtoId<TagPrototype> HealTag = "EmitterBolt";

    [DataField]
    public float HealAmount = 0.1f;

    [DataField]
    public ProtoId<TagPrototype> SupermatterStopTag = "SupermatterStop";


    [DataField]
    public SoundSpecifier StopSoundPath = new SoundCollectionSpecifier("Supermatter");


    [DataField]
    public TimeSpan CatastropheLightningInterval = TimeSpan.FromSeconds(1.0);

    [DataField]
    [AutoPausedField]
    public TimeSpan CatastropheLightningNextTime = TimeSpan.Zero;

    [DataField]
    public float CatastropheLightningRange = 15f;

    [DataField]
    public int CatastropheLightningCount = 3;


    [DataField]
    public List<SupermatterAmbientSoundEntry> AmbientSound = new()
    {
        new() { Volume = 0f, Range = 5f },
        new() { Volume = -10f, Range = 3f },
    };
}

[DataDefinition]
public sealed partial class SupermatterIntegrityLevel
{
    [DataField]
    public float Threshold;

    [DataField]
    public Color Color = Color.White;

    [DataField]
    public LocId Description = string.Empty;

    [DataField]
    public LocId Warning = string.Empty;

    [DataField]
    public bool Flag;
}

[DataDefinition]
public sealed partial class SupermatterAmbientSoundEntry
{
    [DataField]
    public float Volume;

    [DataField]
    public float Range;
}
