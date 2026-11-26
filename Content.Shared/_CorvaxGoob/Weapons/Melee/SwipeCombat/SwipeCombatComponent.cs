using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CorvaxGoob.Weapons.Melee.SwipeCombat;

/// <summary>
/// Боевка «как в Daggerfall»: зажать ЛКМ и провести мышью в сторону.
/// Направление свайпа относительно персонажа выбирает тип атаки. Обычных лёгких и тяжёлых атак нет.
/// Все множители считаются от параметров <see cref="Content.Shared.Weapons.Melee.MeleeWeaponComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SwipeCombatComponent : Component
{
    /// <summary>Минимальная длина свайпа в пикселях мыши, чтобы он считался атакой.</summary>
    [DataField, AutoNetworkedField]
    public float DragThreshold = 25f;

    /// <summary>Сущность-эффект для анимации удара. По умолчанию её спрайт — монтировка с синим контуром (особый удар);
    /// для обычного удара клиент копирует на неё спрайт оружия.</summary>
    [DataField, AutoNetworkedField]
    public EntProtoId Effect = "SwipeCombatEffect";

    /// <summary>Сколько секунд держится серия на цели.</summary>
    [DataField]
    public TimeSpan ComboWindow = TimeSpan.FromSeconds(5);

    /// <summary>Максимум целей у размашистых ударов.</summary>
    [DataField]
    public int MaxSlashTargets = 5;

    // --- Сверху вниз ---

    [DataField]
    public float OverheadRangeMultiplier = 1.5f;

    [DataField]
    public Angle OverheadAngle = Angle.FromDegrees(15);

    /// <summary>На каком расстоянии от атакующего останавливается притянутая цель.</summary>
    [DataField]
    public float PullStopDistance = 1f;

    [DataField]
    public float PullSpeed = 8f;

    /// <summary>Длительность падения от особого удара сверху вниз.</summary>
    [DataField]
    public TimeSpan ComboKnockdownTime = TimeSpan.FromSeconds(1.5);

    // --- Снизу вверх ---

    [DataField]
    public float ThrustRangeMultiplier = 0.75f;

    [DataField]
    public Angle ThrustAngle = Angle.FromDegrees(15);

    [DataField]
    public float ThrustDamageMultiplier = 1.5f;

    // --- В стороны ---

    [DataField]
    public float SlashRangeMultiplier = 1f;

    [DataField]
    public Angle SlashAngle = Angle.FromDegrees(140);

    [DataField]
    public float SlashFirstDamageMultiplier = 1f;

    [DataField]
    public float SlashFollowupDamageMultiplier = 0.5f;

    /// <summary>Отбрасывание обычного размаха в тайлах (0 — без отбрасывания).</summary>
    [DataField]
    public float SlashKnockback = 0f;

    [DataField]
    public float ComboSlashKnockback = 2f;

    [DataField]
    public float KnockbackSpeed = 7f;

    [DataField]
    public float ComboStaminaDamage = 15f;
}
