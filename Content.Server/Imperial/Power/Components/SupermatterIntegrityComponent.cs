using Content.Shared.Damage;
using Content.Shared.Radio;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using Content.Shared.Explosion;

namespace Content.Server.Imperial.Power.Components;

[RegisterComponent]
public sealed partial class SupermatterIntegrityComponent : Component
{
    /// <summary>
    /// Описание кристалла и оповещения в зависимости от состояния.
    /// Каждый элемент списка содержит:
    /// <list type="point">
    /// <item>
    /// <term>Threshold </term>
    /// <description>Значение целостности кристалла (%) при котором активен этот уровень.</description>
    /// </item>
    /// <item>
    /// <term>Color </term>
    /// <description>Цвет описания консоли мониторинга суперматерии.</description>
    /// </item>
    /// <item>
    /// <term>Description </term>
    /// <description>LocId строки с описанием состояния.</description>
    /// </item>
    /// <item>
    /// <term>Warning </term>
    /// <description>LocId предупреждения для отправки в рацию.</description>
    /// </item>
    /// <item>
    /// <term>Flag </term>
    /// <description>Флаг, указывающий, отправлялось ли предупреждение.</description>
    /// </item>
    /// </list>
    /// </summary>
    public List<(float Threshold, Color Color, LocId Description, LocId Warning, bool Flag)> SupermatterIntegrity =
    [
        (95f, Color.Green, "supermatter-desc-pristine", "supermatter-warn-95", false),
        (75f, Color.Yellow, "supermatter-desc-scratched", "supermatter-warn-75", false),
        (50f, Color.Orange, "supermatter-desc-cracked", "supermatter-warn-50", false),
        (25f, Color.Brown, "supermatter-desc-badly-cracked", "supermatter-warn-25", false),
        (10f, Color.DarkRed, "supermatter-desc-critical", "supermatter-warn-10", false),
        (0f, Color.Red, "", "", false),
    ];

    /// <summary>
    /// Активирована ли суперматерия.
    /// До активации она не выполняет никаких процессов.
    /// </summary>
    [DataField]
    public bool Activated;

    /// <summary>
    /// Текущая целостность кристалла
    /// </summary>
    [DataField]
    public float Integrity = 100f;

    /// <summary>
    /// Максимальная целостность
    /// </summary>
    [DataField]
    public float MaxIntegrity = 100f;

    /// <summary>
    /// Сколько урона наносится за тик при опасных условиях
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public DamageSpecifier TickDamage = new();

    /// <summary>
    /// Интервал между тиками урона
    /// </summary>
    [DataField]
    public TimeSpan DamageTickInterval = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Индивидуальный таймер для тиков урона
    /// </summary>
    public TimeSpan TickAccumulator = TimeSpan.Zero;

    /// <summary>
    /// Минимальная целостность, при которой начинается катастрофа
    /// </summary>
    [DataField]
    public float CatastropheThreshold;

    /// <summary>
    /// Канал радио, в который будут отправляться оповещения
    /// </summary>
    [DataField]
    public ProtoId<RadioChannelPrototype> RadioChannel = "Engineering";

    /// <summary>
    /// Активна ли катастрофа
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool CatastropheActive = false;

    /// <summary>
    /// Верхняя граница температуры, после которой наступают плохие для суперматерии условия
    /// </summary>
    public readonly float UpperTempThreshold = 350f;

    /// <summary>
    /// Нижняя граница температуры, после которой наступают плохие для суперматерии условия
    /// </summary>
    public readonly float LowerTempThreshold = 250f;

    /// <summary>
    /// Верхняя граница давления, после которой наступают плохие для суперматерии условия
    /// </summary>
    public readonly float UpperPressureThreshold = 300f;

    /// <summary>
    /// Нижняя граница давления, после которой наступают плохие для суперматерии условия
    /// </summary>
    public readonly float LowerPressureThreshold = 10f;

    /// <summary>
    /// Таймер катастрофы
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan CatastropheTimer = TimeSpan.Zero;

    /// <summary>
    /// Продолжительность катастрофы до финального события (взрыва).
    /// </summary>
    [DataField]
    public TimeSpan CatastropheDuration = TimeSpan.FromSeconds(120);

    /// <summary>
    /// Тег, прототипы с которым лечат Суперматерию
    /// </summary>
    [DataField]
    public ProtoId<TagPrototype> HealTag = "EmitterBolt";

    /// <summary>
    /// Количество здоровья, восстанавливаемое за один выстрел эмиттера
    /// </summary>
    [DataField]
    public float EmitterHealAmount = 0.1f;

    /// <summary>
    /// Идентификатор прототипа взрыва, который будет использован при катастрофе.
    /// </summary>
    [DataField]
    public ProtoId<ExplosionPrototype> ExplosionPrototypeId = "Supermatter";

    /// <summary>
    /// Общая интенсивность взрыва при катастрофе.
    /// </summary>
    [DataField]
    public float CatastropheTotalIntensity = 2500f;

    /// <summary>
    /// Крутизна спадания интенсивности взрыва.
    /// </summary>
    [DataField]
    public float CatastropheSlope = 1f;

    /// <summary>
    /// Максимальная интенсивность на тайле для взрыва.
    /// </summary>
    [DataField]
    public float CatastropheMaxTileIntensity = 35f;

    /// <summary>
    /// Интервал между молниями во время катастрофы
    /// </summary>
    [DataField]
    public TimeSpan CatastropheLightningInterval = TimeSpan.FromSeconds(1.0);

    /// <summary>
    /// Таймер для молний во время катастрофы
    /// </summary>
    [DataField]
    public TimeSpan CatastropheLightningTimer = TimeSpan.Zero;

    /// <summary>
    /// Дальность молний во время катастрофы
    /// </summary>
    [DataField]
    public float CatastropheLightningRange = 15f;

    /// <summary>
    /// Количество молний за раз во время катастрофы
    /// </summary>
    [DataField]
    public int CatastropheLightningCount = 3;

    /// <summary>
    /// Настройки звука для включённой [0] и выключенной [1] суперматерии
    /// </summary>
    [DataField]
    public List<(float Volume, float Range)> AmbientSound =
    [
        (0f, 5f),
        (-10f, 3f),
    ];
}
