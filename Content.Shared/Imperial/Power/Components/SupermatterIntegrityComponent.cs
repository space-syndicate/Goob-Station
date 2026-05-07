using Content.Shared.Damage;
using Content.Shared.Explosion;
using Content.Shared.Radio;
using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.Imperial.Power.Components;

[RegisterComponent]
public sealed partial class SupermatterIntegrityComponent : Component
{
    public List<(float Threshold, Color Color, LocId Description, LocId Warning, bool Flag)> SupermatterIntegrity = new()
    {
        (95f, Color.Green, "supermatter-desc-pristine", "supermatter-warn-95", false),
        (75f, Color.Yellow, "supermatter-desc-scratched", "supermatter-warn-75", false),
        (50f, Color.Orange, "supermatter-desc-cracked", "supermatter-warn-50", false),
        (25f, Color.Brown, "supermatter-desc-badly-cracked", "supermatter-warn-25", false),
        (10f, Color.DarkRed, "supermatter-desc-critical", "supermatter-warn-10", false),
        (0f, Color.Red, "", "", false),
    };

    [DataField]
    public bool Activated;

    [DataField]
    public float Integrity = 100f;

    [DataField]
    public float MaxIntegrity = 100f;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public DamageSpecifier TickDamage = new();

    [DataField]
    public TimeSpan DamageTickInterval = TimeSpan.FromSeconds(1);

    public TimeSpan TickAccumulator = TimeSpan.Zero;

    [DataField]
    public float CatastropheThreshold;

    [DataField]
    public ProtoId<RadioChannelPrototype> RadioChannel = "Engineering";

    [ViewVariables(VVAccess.ReadWrite)]
    public bool CatastropheActive;

    public readonly float UpperTempThreshold = 350f;
    public readonly float LowerTempThreshold = 250f;
    public readonly float UpperPressureThreshold = 300f;
    public readonly float LowerPressureThreshold = 10f;

    [ViewVariables(VVAccess.ReadOnly)]
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
    public TimeSpan CatastropheLightningTimer = TimeSpan.Zero;

    [DataField]
    public float CatastropheLightningRange = 15f;

    [DataField]
    public int CatastropheLightningCount = 3;

    [DataField]
    public List<(float Volume, float Range)> AmbientSound = new()
    {
        (0f, 5f),
        (-10f, 3f),
    };
}

