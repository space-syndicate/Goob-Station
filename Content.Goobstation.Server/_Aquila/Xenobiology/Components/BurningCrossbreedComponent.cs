using Content.Goobstation.Server._Aquila.Xenobiology.Systems;
using Content.Shared.EntityEffects;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Server._Aquila.Xenobiology.Components;

[RegisterComponent, Access(typeof(BurningCrossbreedSystem))]
public sealed partial class BurningCrossbreedComponent : Component
{
    /// <summary>
    /// Эффекты, применяемые вокруг гибрида при срабатывании.
    /// </summary>
    [DataField(required: true)]
    public EntityEffect[] Effects = default!;

    /// <summary>
    /// Через сколько после активации гибрид срабатывает.
    /// </summary>
    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Звук при активации гибрида.
    /// </summary>
    [DataField]
    public SoundSpecifier? ActivateSound = new SoundPathSpecifier("/Audio/Effects/Chemistry/bubbles.ogg");

    /// <summary>
    /// Визуальный эффект при активации гибрида.
    /// </summary>
    [DataField]
    public EntProtoId? ActivateEffect = "EffectXenobioBoil";

    /// <summary>
    /// Визуальный эффект при срабатывании гибрида.
    /// </summary>
    [DataField]
    public EntProtoId? Effect = "EffectXenobioBurning";

    /// <summary>
    /// Амплитуда дрожания активированного гибрида.
    /// </summary>
    [DataField]
    public float JitterAmplitude = 15f;

    /// <summary>
    /// Частота дрожания активированного гибрида.
    /// </summary>
    [DataField]
    public float JitterFrequency = 20f;
}
