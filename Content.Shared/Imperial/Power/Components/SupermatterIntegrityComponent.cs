using Content.Shared.Damage;
using Content.Shared.Explosion;
using Content.Shared.Radio;
using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Imperial.Power.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
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

    [DataField, AutoNetworkedField]
    public float Integrity = 100f;

    [DataField, AutoNetworkedField]
    public float MaxIntegrity = 100f;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public DamageSpecifier TickDamage = new();

    [DataField]
    public TimeSpan DamageTickInterval = TimeSpan.FromSeconds(1);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextDamageTick;

    [DataField]
    public float CatastropheThreshold;

    [DataField]
    public ProtoId<RadioChannelPrototype> RadioChannel = "Engineering";

    [ViewVariables(VVAccess.ReadWrite)]
    public bool CatastropheActive;

    [DataField]
    public float UpperTempThreshold = 350f;

    [DataField]
    public float LowerTempThreshold = 250f;

    [DataField]
    public float UpperPressureThreshold = 300f;

    [DataField]
    public float LowerPressureThreshold = 10f;

    [ViewVariables(VVAccess.ReadWrite)]
    [AutoPausedField]
    public TimeSpan CatastropheTimer = TimeSpan.Zero;

    [DataField]
    public TimeSpan CatastropheDuration = TimeSpan.FromSeconds(120);

    [DataField]
    public ProtoId<TagPrototype> HealTag = "EmitterBolt";

    [DataField]
    public ProtoId<TagPrototype> SupermatterStopTag = "SupermatterStop";

    [DataField]
    public SoundPathSpecifier ShutdownSoundPath = new("/Audio/Imperial/Power/Supermatter/supermatter_power_off.ogg");

    [DataField]
    public float EmitterHealAmount = 0.1f;

    [DataField]
    public ProtoId<ExplosionPrototype> ExplosionPrototypeId = "Supermatter";

    [DataField]
    public float CatastropheTotalIntensity = 2500f;

    [DataField]
    public float CatastropheSlope = 1f;

    [DataField]
    public float CatastropheMaxTileIntensity = 35f;

    [DataField]
    public TimeSpan CatastropheLightningInterval = TimeSpan.FromSeconds(1.0);

    [DataField]
    [AutoPausedField]
    public TimeSpan CatastropheLightningTimer = TimeSpan.Zero;

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
