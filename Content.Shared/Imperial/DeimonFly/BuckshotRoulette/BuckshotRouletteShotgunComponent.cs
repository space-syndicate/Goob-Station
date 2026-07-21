using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Imperial.DeimonFly.BuckshotRoulette;

/// <summary>
/// Отмечает дробовик Buckshot Roulette, хранит его режим и параметры двух типов патронов.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true, fieldDeltas: true)]
public sealed partial class BuckshotRouletteShotgunComponent : Component
{
    [DataField(required: true)]
    public EntProtoId LiveShell = string.Empty;

    [DataField(required: true)]
    public EntProtoId BlankShell = string.Empty;

    /// <summary>
    /// Определяет, полетит ли следующий снаряд по прицелу или будет применён к стрелку.
    /// </summary>
    [DataField, AutoNetworkedField]
    public BuckshotRouletteFireMode FireMode = BuckshotRouletteFireMode.Target;

    /// <summary>
    /// Урон боевого патрона в режиме выстрела в себя.
    /// </summary>
    [DataField(required: true)]
    public DamageSpecifier SelfDamage = new();

    /// <summary>
    /// Множитель урона следующего выстрела после применения пилы.
    /// </summary>
    [DataField]
    public float SawDamageMultiplier = 2f;

    /// <summary>
    /// Резервный звук извлечения патрона под действием пива.
    /// </summary>
    [DataField]
    public SoundSpecifier ShellEjectSound = new SoundCollectionSpecifier("ShellEject");

    /// <summary>
    /// Удваивает урон следующего фактически использованного патрона и сбрасывается после выстрела.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool DoubleNextShot;

    /// <summary>
    /// Текущее визуальное состояние ствола, синхронизируемое с клиентами.
    /// </summary>
    [DataField, AutoNetworkedField]
    public BuckshotRouletteBarrelVisualState BarrelVisualState = BuckshotRouletteBarrelVisualState.Intact;

    /// <summary>
    /// Обычный RSI дробовика, используемый после полного восстановления ствола.
    /// </summary>
    [DataField(required: true)]
    public ResPath IntactWorldSprite;

    /// <summary>
    /// RSI обрезанного дробовика для мира и слотов экипировки.
    /// </summary>
    [DataField(required: true)]
    public ResPath SawedWorldSprite;

    /// <summary>
    /// RSI с одноразовой анимацией восстановления ствола.
    /// </summary>
    [DataField(required: true)]
    public ResPath RestoringWorldSprite;

    /// <summary>
    /// RSI обрезанного дробовика для отображения в руках.
    /// </summary>
    [DataField(required: true)]
    public ResPath SawedInhandSprite;

    /// <summary>
    /// Время от начала анимации до переключения на целый статичный спрайт.
    /// </summary>
    [DataField]
    public TimeSpan BarrelRestoreDuration = TimeSpan.FromSeconds(1.85);

    /// <summary>
    /// После усиленного выстрела восстановление ожидает фактического выбрасывания оружия из рук.
    /// </summary>
    [ViewVariables]
    public bool BarrelRestorationPending;

    /// <summary>
    /// Серверное время окончания уже запущенной анимации.
    /// </summary>
    [ViewVariables]
    public TimeSpan? BarrelRestoreAt;

    /// <summary>
    /// Последнее состояние ствола, применённое клиентской системой визуализации.
    /// </summary>
    [ViewVariables]
    public BuckshotRouletteBarrelVisualState? AppliedBarrelVisualState;
}
