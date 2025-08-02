using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Content.Shared.Radio;
using Robust.Shared.Random;
using Content.Server.Imperial.ImperialLightning;

namespace Content.Server.Imperial.Power.Components
{
    public enum SupermatterEventType
    {
        None = 0,
        Lightning = 1,
        Radiation = 2,
        Plasma = 3
    }

    [RegisterComponent]
    public sealed partial class SupermatterEventComponent : Component
    {
        // Время до следующего случайного события (секунды)
        [DataField]
        public TimeSpan NextEventTimer = TimeSpan.Zero;
        // Тип текущего события (0 - ничего, 1 - молнии, 2 - радиация, 3 - плазма)
        [DataField]
        public SupermatterEventType CurrentEvent = SupermatterEventType.None;
        // Время окончания текущего события (секунды, если 0 - нет активного события)
        [DataField]
        public TimeSpan EventEndTime = TimeSpan.Zero;
        // Кулдаун для всплеска молний
        [DataField]
        public TimeSpan LightningCooldown = TimeSpan.Zero;
        // Таймер для генерации плазмы во время PlasmaEvent
        [DataField]
        public TimeSpan? PlasmaTickAccumulator = null;
        // Допустимые типы событий для этого кристалла
        [DataField]
        public List<SupermatterEventType> AllowedEventTypes { get; set; } = new()
        {
            SupermatterEventType.None,
            SupermatterEventType.Lightning,
            SupermatterEventType.Radiation,
            SupermatterEventType.Plasma
        };
        // Каналы рации для оповещений
        [DataField]
        public ProtoId<RadioChannelPrototype>[] RadioChannels = { "Engineering" };

        // Время до первого ивента (секунды)
        [DataField]
        public float InitialEventDelaySeconds = 900f; // 15 минут

        // Время жизни кэша консоли (секунды)
        [DataField]
        public float ConsoleCacheLifetime = 10f;

        // Настройки событий
        [DataField]
        public float NoneEventDuration = 300f; // 5 минут для None события

        [DataField]
        public float LightningEventDuration = 120f; // 2 минуты для Lightning события

        [DataField]
        public float LightningCooldownDuration = 2f; // 2 секунды кулдаун для молний

        [DataField]
        public float LightningSpawnDuration = 1f; // 1 секунда для спавна молнии

        [DataField]
        public float LightningMinNextEvent = 180f; // 3 минуты минимальное время до следующего события

        [DataField]
        public float LightningMaxNextEvent = 420f; // 7 минут максимальное время до следующего события

        [DataField]
        public float RadiationEventDuration = 120f; // 2 минуты для Radiation события

        [DataField]
        public float RadiationIntensity = 10f; // Интенсивность радиации во время события

        [DataField]
        public float RadiationMinNextEvent = 180f; // 3 минуты минимальное время до следующего события

        [DataField]
        public float RadiationMaxNextEvent = 420f; // 7 минут максимальное время до следующего события

        [DataField]
        public float PlasmaEventDuration = 120f; // 2 минуты для Plasma события

        [DataField]
        public float PlasmaMinNextEvent = 180f; // 3 минуты минимальное время до следующего события

        [DataField]
        public float PlasmaMaxNextEvent = 420f; // 7 минут максимальное время до следующего события

        [DataField]
        public float PlasmaTickInterval = 10f; // 10 секунд интервал для генерации плазмы

        [DataField]
        public float PlasmaMolesAmount = 5f; // Количество молей плазмы и кислорода для генерации

        [DataField]
        public float PlasmaHotspotTemperature = 1500f; // Температура хотспота плазмы

        [DataField]
        public float PlasmaHotspotVolume = 50f; // Объем хотспота плазмы

        [DataField]
        public float DefaultRadiationIntensity = 5f; // Интенсивность радиации по умолчанию

        // Время кэша консоли (используется системой)
        public TimeSpan ConsoleCacheTimer = TimeSpan.Zero;

        // Время последнего обновления для различных таймеров
        public TimeSpan LastConsoleCacheUpdate = TimeSpan.Zero;
        public TimeSpan LastEventEndTimeUpdate = TimeSpan.Zero;
        public TimeSpan LastNextEventTimerUpdate = TimeSpan.Zero;
        public TimeSpan LastLightningCooldownUpdate = TimeSpan.Zero;
        public TimeSpan LastPlasmaTickUpdate = TimeSpan.Zero;
    }
}
