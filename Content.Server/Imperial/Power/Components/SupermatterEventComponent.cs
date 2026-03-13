using Robust.Shared.Prototypes;
using Content.Shared.Radio;
using Content.Server.Imperial.Power.EntitySystems.Events;
using Robust.Shared.Random;

namespace Content.Server.Imperial.Power.Components;

[RegisterComponent]
public sealed partial class SupermatterEventComponent : Component
{
    /// <summary>
    /// Значения видов событий суперматерии.
    /// </summary>
    public enum SupermatterEventType
    {
        None = 0,
        Lightning = 1,
        Radiation = 2,
        Plasma = 3,
    }

    /// <summary>
    /// Список разрешенных типов событий для этого кристалла.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public List<SupermatterEventType> AllowedEventTypes { get; set; } =
    [
        SupermatterEventType.None,
        SupermatterEventType.Lightning,
        SupermatterEventType.Radiation,
        SupermatterEventType.Plasma,
    ];

    /// <summary>
    /// Тип события в само событие.
    /// </summary>
    public readonly Dictionary<SupermatterEventType, object> SupermatterEventTypesToEvents = new()
    {
        { SupermatterEventType.None, new SupermatterNoneEvent() },
        { SupermatterEventType.Lightning, new SupermatterLightningEvent() },
        { SupermatterEventType.Radiation, new SupermatterRadiationEvent() },
        { SupermatterEventType.Plasma, new SupermatterPlasmaEvent() },
    };

    /// <summary>
    /// Объявлена ли война Ядерными Оперативниками. Если да - не создавать события.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public bool IsWarOps = false;

    /// <summary>
    /// Время до первого события, далее устанавливается самими событиями (в секундах).
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextEventTimer = TimeSpan.FromSeconds(Random.Shared.Next(600, 900));

    /// <summary>
    /// Текущий активный тип события.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public SupermatterEventType CurrentEvent = SupermatterEventType.None;

    /// <summary>
    /// Время окончания текущего события (0, если событие не активно).
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan EventEndTime = TimeSpan.Zero;

    /// <summary>
    /// Кулдаун между всплесками молний.
    /// </summary>
    public TimeSpan LightningCooldown = TimeSpan.Zero;

    /// <summary>
    /// Таймер для генерации плазмы во время события Plasma.
    /// </summary>
    public TimeSpan? PlasmaTickAccumulator = null;

    /// <summary>
    /// Радио каналы для оповещений о событиях.
    /// </summary>
    [DataField]
    public ProtoId<RadioChannelPrototype>[] RadioChannels = ["Engineering"];

    /// <summary>
    /// Время жизни кэша консоли (в секундах).
    /// </summary>
    public readonly TimeSpan ConsoleCacheLifetime = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Длительность события None (в секундах).
    /// </summary>
    [DataField]
    public TimeSpan NoneEventDuration = TimeSpan.FromSeconds(Random.Shared.Next(200, 300));

    // LightningEvent

    /// <summary>
    /// Длительность события LightningEvent (в секундах).
    /// </summary>
    [DataField]
    public TimeSpan LightningEventDuration = TimeSpan.FromSeconds(Random.Shared.Next(80, 120));

    /// <summary>
    /// Кулдаун между молниями во время события LightningEvent (в секундах).
    /// </summary>
    [DataField]
    public TimeSpan LightningCooldownDuration = TimeSpan.FromSeconds(Random.Shared.Next(5, 8));

    /// <summary>
    /// Время до следующего события после LightningEvent (в секундах).
    /// </summary>
    public readonly TimeSpan EventAfterLightingTime = TimeSpan.FromSeconds(Random.Shared.Next(180, 420));

    /// <summary>
    /// Количество молний, выпускаемых за один раз при LightningEvent.
    /// </summary>
    [DataField]
    public int LightningBoltCount = 1;

    /// <summary>
    /// Радиус, в котором молнии будут выпускаться при LightingEvent.
    /// </summary>
    [DataField]
    public float LightningBoltRadius = 8f;

    // RadiationEvent

    /// <summary>
    /// Длительность RadiationEvent (в секундах).
    /// </summary>
    [DataField]
    public TimeSpan RadiationEventDuration = TimeSpan.FromSeconds(Random.Shared.Next(80, 120));

    /// <summary>
    /// Интенсивность радиации во время события RadiationEvent.
    /// </summary>
    [DataField]
    public float RadiationEventIntensity = Random.Shared.NextFloat(6f, 12f);

    /// <summary>
    /// Время до следующего события после RadiationEvent (в секундах).
    /// </summary>
    public readonly TimeSpan EventAfterRadiationTime = TimeSpan.FromSeconds(Random.Shared.Next(180, 420));

    // PlasmaEvent

    /// <summary>
    /// Длительность PlasmaEvent (в секундах).
    /// </summary>
    [DataField]
    public TimeSpan PlasmaEventDuration = TimeSpan.FromSeconds(Random.Shared.Next(80, 120));

    /// <summary>
    /// Время до следующего события после PlasmaEvent (в секундах).
    /// </summary>
    public readonly TimeSpan EventAfterPlasmaTime = TimeSpan.FromSeconds(Random.Shared.Next(140, 180));

    /// <summary>
    /// Интервал генерации плазмы во время PlasmaEvent (в секундах).
    /// </summary>
    public readonly TimeSpan PlasmaTickInterval = TimeSpan.FromSeconds(Random.Shared.Next(6, 10));

    /// <summary>
    /// Количество молей плазмы, генерируемых за тик.
    /// </summary>
    [DataField]
    public float PlasmaMolesAmount = Random.Shared.NextFloat(3f, 7f);

    /// <summary>
    /// Температура хотспота плазмы при PlasmaEvent.
    /// </summary>
    [DataField]
    public float PlasmaHotspotTemperature = Random.Shared.NextFloat(1250f, 1500f);

    /// <summary>
    /// Объем хотспота плазмы при PlasmaEvent.
    /// </summary>
    [DataField]
    public float PlasmaHotspotVolume = Random.Shared.NextFloat(35f, 50f);

    // Default radiation

    /// <summary>
    /// Базовая интенсивность радиации вне событий.
    /// </summary>
    [DataField]
    public float DefaultRadiationIntensity = Random.Shared.NextFloat(4, 5);

    public TimeSpan LastConsoleCacheUpdate = TimeSpan.Zero;
    public TimeSpan LastEventEndTimeUpdate = TimeSpan.Zero;
    public TimeSpan LastNextEventTimerUpdate = TimeSpan.Zero;
    public TimeSpan LastLightningCooldownUpdate = TimeSpan.Zero;
    public TimeSpan LastPlasmaTickUpdate = TimeSpan.Zero;
}
