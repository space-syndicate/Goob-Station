using Content.Goobstation.Maths.FixedPoint;
using Content.Goobstation.Server._Aquila.Xenobiology.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Goobstation.Server._Aquila.Xenobiology.Components;

[RegisterComponent, AutoGenerateComponentPause, Access(typeof(SlimeVoiceSystem))]
public sealed partial class SlimeVoiceComponent : Component
{
    /// <summary>
    /// Звук, когда слайма гладят.
    /// </summary>
    [DataField]
    public SoundSpecifier? PettedSound = new SoundPathSpecifier("/Audio/_Aquila/Voice/Slime/slime_bubble.ogg", AudioParams.Default.WithVariation(0.1f).WithPitchScale(1.2f));

    /// <summary>
    /// Звук, когда слайм проголодался.
    /// </summary>
    [DataField]
    public SoundSpecifier? HungrySound = new SoundPathSpecifier("/Audio/Voice/Talk/slime_ask.ogg", AudioParams.Default.WithVariation(0.1f));

    /// <summary>
    /// Звук, когда слайм начинает голодать.
    /// </summary>
    [DataField]
    public SoundSpecifier? StarvingSound = new SoundPathSpecifier("/Audio/Voice/Talk/slime_exclaim.ogg", AudioParams.Default.WithVariation(0.1f));

    /// <summary>
    /// Звук, когда слайм получает урон.
    /// </summary>
    [DataField]
    public SoundSpecifier? HurtSound = new SoundPathSpecifier("/Audio/Voice/Slime/slime_squish.ogg", AudioParams.Default.WithVariation(0.1f));

    /// <summary>
    /// Звук при смерти слайма.
    /// </summary>
    [DataField]
    public SoundSpecifier? DeathSound = new SoundPathSpecifier("/Audio/_EinsteinEngines/Voice/Slime/slime_schlorp.ogg", AudioParams.Default.WithPitchScale(0.8f));

    /// <summary>
    /// Минимальный урон, на который слайм реагирует, чтобы пассивный урон вроде удушья не озвучивался.
    /// </summary>
    [DataField]
    public FixedPoint2 MinHurtDamage = 1;

    /// <summary>
    /// Сколько слайм должен не получать урон, чтобы звук боли мог сыграть снова.
    /// </summary>
    [DataField]
    public TimeSpan HurtCooldown = TimeSpan.FromSeconds(1.5);

    /// <summary>
    /// Когда звук боли сможет сыграть снова.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextHurtSound;

    /// <summary>
    /// Сколько слайма не должны гладить, чтобы звук поглаживания мог сыграть снова.
    /// </summary>
    [DataField]
    public TimeSpan PettedCooldown = TimeSpan.FromSeconds(4);

    /// <summary>
    /// Когда звук поглаживания сможет сыграть снова.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextPettedSound;
}
