using Content.Server.AlertLevel;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Chat.Systems;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Imperial.Power.Components;
using Content.Server.Lightning;
using Content.Server.Radio.EntitySystems;
using Content.Server.Station.Systems;
using Content.Shared.Audio;
using Content.Shared.Examine;
using Content.Shared.Radiation.Components;
using Content.Shared.Tag;
using Robust.Shared.Physics.Events;
using System.Linq;
using Robust.Server.GameObjects;

namespace Content.Server.Imperial.Power.EntitySystems;

public sealed class SupermatterIntegritySystem : EntitySystem
{
    [Dependency] private readonly AlertLevelSystem _alertLevelSystem = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly ExplosionSystem _explosionSystem = default!;
    [Dependency] private readonly LightningSystem _lightning = default!;
    [Dependency] private readonly SharedAmbientSoundSystem _ambientSound = default!;
    [Dependency] private readonly SharedPointLightSystem _lightSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly RadioSystem _radioSystem = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SupermatterIntegrityComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<SupermatterIntegrityComponent, StartCollideEvent>(OnStartCollide);
    }

    private void OnExamined(EntityUid uid, SupermatterIntegrityComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(component.Activated
            ? $"[color=yellow]{Loc.GetString("supermatter-status-active")}[/color]"
            : $"[color=gray]{Loc.GetString("supermatter-status-inactive")}[/color]");

        var integrityPercent = component.Integrity / component.MaxIntegrity * 100;
        var integrityLevel = component.SupermatterIntegrity.First(entry => integrityPercent > entry.Threshold);

        args.PushMarkup(Loc.GetString(integrityLevel.Description));
    }

    private void OnStartCollide(EntityUid uid, SupermatterIntegrityComponent component, ref StartCollideEvent args)
    {
        var other = args.OtherEntity;
        if (!_tagSystem.HasTag(other, component.HealTag))
            return;

        if (!component.Activated)
        {
            component.Activated = true;
            SendSupermatterRadio(args.OurEntity,Loc.GetString("supermatter-activated"), component);
        }

        component.Integrity = MathF.Min(component.MaxIntegrity, component.Integrity + component.EmitterHealAmount);
    }


    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var enumerator = EntityQueryEnumerator<SupermatterIntegrityComponent, TransformComponent>();
        while (enumerator.MoveNext(out var uid, out var comp, out var transComp))
        {
            ProcessSupermatterUpdate(uid, comp, transComp, frameTime);
        }
    }

    private void ProcessSupermatterUpdate(EntityUid uid, SupermatterIntegrityComponent comp, TransformComponent transComp, float frameTime)
    {
        if (TryComp(uid, out RadiationSourceComponent? radiation))
            radiation.Enabled = comp.Activated;

        if (TryComp(uid, out PointLightComponent? light))
        {
            _lightSystem.SetEnabled(uid, comp.Activated, light);
        }

        if (TryComp(uid, out AmbientSoundComponent? ambient))
        {
            if (comp.Activated)
            {
                _ambientSound.SetVolume(uid, comp.AmbientSound[0].Volume, ambient);
                _ambientSound.SetRange(uid, comp.AmbientSound[0].Range, ambient);
            }
            else
            {
                _ambientSound.SetVolume(uid, comp.AmbientSound[1].Volume, ambient);
                _ambientSound.SetRange(uid, comp.AmbientSound[1].Range, ambient);
            }
        }

        if (!comp.Activated)
            return;

        var gas = _atmosphereSystem.GetContainingMixture((uid, transComp), true, true);

        bool badConditions;
        if (gas != null)
        {
            badConditions = gas.Temperature > comp.UpperTempThreshold
                            || gas.Temperature < comp.LowerTempThreshold
                            || gas.Pressure > comp.UpperPressureThreshold
                            || gas.Pressure < comp.LowerPressureThreshold;
        }
        else
            badConditions = true;

        var integrityPercent = comp.Integrity / comp.MaxIntegrity * 100f;

        // Сброс флага предупреждения для текущего уровня
        var index = comp.SupermatterIntegrity.FindIndex(entry => integrityPercent > entry.Threshold);
        if (index >= 0)
        {
            var oldEntry = comp.SupermatterIntegrity[index];
            if (oldEntry.Flag)
                comp.SupermatterIntegrity[index] = (oldEntry.Threshold, oldEntry.Color, oldEntry.Description, oldEntry.Warning, false);
        }

        foreach (var level in comp.SupermatterIntegrity.OrderByDescending(entry => entry.Threshold))
        {
            if (integrityPercent > level.Threshold || level.Flag || string.IsNullOrEmpty(level.Warning))
                continue;

            var integrityWarning = Loc.GetString(level.Warning);
            SendSupermatterRadio(uid, integrityWarning, comp);

            // Если мы достигли уровня с порогом <= 10% — выставляем код тревоги для станции и объявление.
            // Раньше использовался MinBy по всем порогам (что возвращало 0) и из-за этого код не ставился.
            if (level.Threshold <= 10f)
            {
                var station = _stationSystem.GetOwningStation(uid, transComp);
                if (station != null)
                {
                    _alertLevelSystem.SetLevel(station.Value, "yellow", true, true, true);
                    _chatSystem.DispatchStationAnnouncement(
                        station.Value,
                        Loc.GetString("supermatter-station-critical"),
                        playDefaultSound: true,
                        colorOverride: Color.Yellow
                    );
                }
            }

            // Устанавливаем флаг предупреждения
            var levelIndex = comp.SupermatterIntegrity.FindIndex(entry => Math.Abs(level.Threshold - entry.Threshold) < 1f);
            if (levelIndex >= 0)
            {
                var updated = comp.SupermatterIntegrity[levelIndex];
                comp.SupermatterIntegrity[levelIndex] = (updated.Threshold, updated.Color, updated.Description, updated.Warning, true);
            }
            break;
        }

        // Обработка катастрофы
        if (!comp.CatastropheActive && integrityPercent <= comp.CatastropheThreshold)
        {
            comp.CatastropheActive = true;
            comp.CatastropheTimer = TimeSpan.Zero; // Начинаем с 0
            comp.CatastropheLightningTimer = TimeSpan.Zero; // Сбрасываем таймер молний

            // Отправляем предупреждение о катастрофе
            var station = _stationSystem.GetOwningStation(uid, transComp);
            if (station != null)
            {
                _alertLevelSystem.SetLevel(station.Value, "red", true, true, true);
                _chatSystem.DispatchStationAnnouncement(
                    station.Value,
                    Loc.GetString("supermatter-station-catastrophe"),
                    playDefaultSound: true,
                    colorOverride: Color.Red
                );
            }

            // Отправляем сообщение в радио о начале катастрофы
            SendSupermatterRadio(uid, Loc.GetString("supermatter-catastrophe-warning"), comp);
        }

        if (comp.CatastropheActive)
        {
            comp.CatastropheTimer += TimeSpan.FromSeconds(frameTime);

            // Молнии во время катастрофы
            comp.CatastropheLightningTimer += TimeSpan.FromSeconds(frameTime);
            if (comp.CatastropheLightningTimer >= comp.CatastropheLightningInterval)
            {
                comp.CatastropheLightningTimer = TimeSpan.Zero;
                _lightning.ShootRandomLightnings(uid, comp.CatastropheLightningRange, comp.CatastropheLightningCount);
            }

            if (comp.CatastropheTimer >= comp.CatastropheDuration)
            {
                if (TryComp(uid, out TransformComponent? xformCat))
                {
                    var coords = _transformSystem.ToMapCoordinates(xformCat.Coordinates);
                    _explosionSystem.QueueExplosion(
                        coords,
                        comp.ExplosionPrototypeId,
                        comp.CatastropheTotalIntensity,
                        comp.CatastropheSlope,
                        comp.CatastropheMaxTileIntensity,
                        cause: uid
                    );
                }
                EntityManager.QueueDeleteEntity(uid);
                return;
            }
        }

        // Обработка урона от плохих условий
        if (badConditions)
        {
            comp.TickAccumulator += TimeSpan.FromSeconds(frameTime);
            while (comp.TickAccumulator >= comp.DamageTickInterval)
            {
                comp.TickAccumulator -= comp.DamageTickInterval;
                var tickAmount = comp.TickDamage.DamageDict.Values.Sum(v => (float)v);
                comp.Integrity = MathF.Max(0, comp.Integrity - tickAmount);
            }
        }
    }

    // Отправка сообщения в общую рацию от имени суперматерии
    private void SendSupermatterRadio(EntityUid source, string message, SupermatterIntegrityComponent component)
    {
        _chatSystem.TrySendInGameICMessage(source, message, InGameICChatType.Speak, ChatTransmitRange.Normal);
        _radioSystem.SendRadioMessage(source, message, component.RadioChannel, source);
    }
}
