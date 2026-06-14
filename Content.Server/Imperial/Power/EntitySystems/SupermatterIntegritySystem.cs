using Content.Server.AlertLevel;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Chat.Systems;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Lightning;
using Content.Server.Radio.EntitySystems;
using Content.Server.Station.Systems;
using Content.Shared.Audio;
using Content.Shared.Imperial.Power.Components;
using Content.Shared.Radiation.Components;
using System.Linq;
using Robust.Server.GameObjects;
using Content.Shared.Chat;
using Robust.Shared.Timing;
using Content.Server.Radiation.Systems;
using Content.Shared.Imperial.Power.Systems;

namespace Content.Server.Imperial.Power.EntitySystems;

public sealed class SupermatterIntegritySystem : EntitySystem
{
    [Dependency] private readonly AlertLevelSystem _alertLevelSystem = null!;
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = null!;
    [Dependency] private readonly ChatSystem _chatSystem = null!;
    [Dependency] private readonly ExplosionSystem _explosionSystem = null!;
    [Dependency] private readonly LightningSystem _lightning = null!;
    [Dependency] private readonly SharedAmbientSoundSystem _ambientSound = null!;
    [Dependency] private readonly SharedPointLightSystem _lightSystem = null!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = null!;
    [Dependency] private readonly RadioSystem _radioSystem = null!;
    [Dependency] private readonly StationSystem _stationSystem = null!;
    [Dependency] private readonly IGameTiming _gameTiming = null!;
    [Dependency] private readonly RadiationSystem _radiationSystem = null!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SupermatterIntegrityComponent, SupermatterSendRadioEvent>(OnSupermatterSendRadioEvent);
    }

    private void OnSupermatterSendRadioEvent(Entity<SupermatterIntegrityComponent> ent, ref SupermatterSendRadioEvent args)
    {
        SendSupermatterRadio(ent, args.Message);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var enumerator = EntityQueryEnumerator<SupermatterIntegrityComponent, TransformComponent>();
        while (enumerator.MoveNext(out var uid, out var comp, out var transComp))
        {
            ProcessSupermatterUpdate((uid, comp), transComp, frameTime);
        }
    }

    private void ProcessSupermatterUpdate(Entity<SupermatterIntegrityComponent> entity, TransformComponent transComp, float frameTime)
    {
        if (TryComp(entity, out RadiationSourceComponent? radiation))
            _radiationSystem.SetSourceEnabled(new Entity<RadiationSourceComponent?>(entity, radiation), entity.Comp.Activated);

        if (TryComp(entity, out PointLightComponent? light))
            _lightSystem.SetEnabled(entity, entity.Comp.Activated, light);

        if (TryComp(entity, out AmbientSoundComponent? ambient))
        {
            if (entity.Comp.Activated)
            {
                _ambientSound.SetVolume(entity, entity.Comp.AmbientSound[0].Volume, ambient);
                _ambientSound.SetRange(entity, entity.Comp.AmbientSound[0].Range, ambient);
            }
            else
            {
                _ambientSound.SetVolume(entity, entity.Comp.AmbientSound[1].Volume, ambient);
                _ambientSound.SetRange(entity, entity.Comp.AmbientSound[1].Range, ambient);
            }
        }

        if (!entity.Comp.Activated)
            return;

        var gas = _atmosphereSystem.GetContainingMixture((entity, transComp), true, true);

        bool badConditions;
        if (gas != null)
        {
            badConditions = gas.Temperature > entity.Comp.UpperTempThreshold
                            || gas.Temperature < entity.Comp.LowerTempThreshold
                            || gas.Pressure > entity.Comp.UpperPressureThreshold
                            || gas.Pressure < entity.Comp.LowerPressureThreshold;
        }
        else
            badConditions = true;

        var integrityPercent = entity.Comp.Integrity / entity.Comp.MaxIntegrity * 100f;

        // Сброс флага предупреждения для текущего уровня
        var index = entity.Comp.SupermatterIntegrity.FindIndex(entry => integrityPercent > entry.Threshold);
        if (index >= 0)
        {
            var oldEntry = entity.Comp.SupermatterIntegrity[index];
            if (oldEntry.Flag)
                oldEntry.Flag = false;
        }

        foreach (var level in entity.Comp.SupermatterIntegrity.OrderByDescending(entry => entry.Threshold))
        {
            if (integrityPercent > level.Threshold || level.Flag || string.IsNullOrEmpty(level.Warning))
                continue;

            var integrityWarning = Loc.GetString(level.Warning);
            SendSupermatterRadio(entity, integrityWarning);

            // Если мы достигли уровня с порогом <= 10% — выставляем код тревоги для станции и объявление.
            // Раньше использовался MinBy по всем порогам (что возвращало 0) и из-за этого код не ставился.
            if (level.Threshold <= 30f)
            {
                var station = _stationSystem.GetOwningStation(entity, transComp);
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
            level.Flag = true;
            break;
        }

        // Обработка катастрофы
        if (!entity.Comp.CatastropheActive && integrityPercent <= entity.Comp.CatastropheThreshold)
        {
            entity.Comp.CatastropheActive = true;
            entity.Comp.CatastropheTimer = TimeSpan.Zero; // Начинаем с 0
            entity.Comp.CatastropheLightningTimer = TimeSpan.Zero; // Сбрасываем таймер молний

            // Отправляем предупреждение о катастрофе
            var station = _stationSystem.GetOwningStation(entity, transComp);
            if (station != null)
            {
                _chatSystem.DispatchStationAnnouncement(
                    station.Value,
                    Loc.GetString("supermatter-station-catastrophe"),
                    playDefaultSound: true,
                    colorOverride: Color.Red
                );
            }
        }

        if (entity.Comp.CatastropheActive)
        {
            entity.Comp.CatastropheTimer += TimeSpan.FromSeconds(frameTime);

            // Молнии во время катастрофы
            entity.Comp.CatastropheLightningTimer += TimeSpan.FromSeconds(frameTime);
            if (entity.Comp.CatastropheLightningTimer >= entity.Comp.CatastropheLightningInterval)
            {
                entity.Comp.CatastropheLightningTimer = TimeSpan.Zero;
                _lightning.ShootRandomLightnings(entity, entity.Comp.CatastropheLightningRange, entity.Comp.CatastropheLightningCount);
            }

            if (entity.Comp.CatastropheTimer >= entity.Comp.CatastropheDuration)
            {
                if (TryComp(entity, out TransformComponent? xformCat))
                {
                    var coords = _transformSystem.ToMapCoordinates(xformCat.Coordinates);
                    _explosionSystem.QueueExplosion(
                        coords,
                        entity.Comp.ExplosionPrototypeId,
                        entity.Comp.CatastropheTotalIntensity,
                        entity.Comp.CatastropheSlope,
                        entity.Comp.CatastropheMaxTileIntensity,
                        cause: entity
                    );
                }
                QueueDel(entity);
                return;
            }
        }

        // Обработка урона от плохих условий
        if (badConditions)
        {
            if (entity.Comp.NextDamageTick == TimeSpan.Zero)
                entity.Comp.NextDamageTick = _gameTiming.CurTime + entity.Comp.DamageTickInterval;

            var tookDamage = false;
            while (_gameTiming.CurTime >= entity.Comp.NextDamageTick)
            {
                entity.Comp.NextDamageTick += entity.Comp.DamageTickInterval;
                var tickAmount = entity.Comp.TickDamage.DamageDict.Values.Sum(v => (float)v);
                entity.Comp.Integrity = MathF.Max(0, entity.Comp.Integrity - tickAmount);
                tookDamage = true;
            }

            if (tookDamage)
                Dirty(entity);
        }
        else
        {
            entity.Comp.NextDamageTick = TimeSpan.Zero;
        }
    }

    // Отправка сообщения в общую рацию от имени суперматерии
    private void SendSupermatterRadio(Entity<SupermatterIntegrityComponent> entity, string message)
    {
        _chatSystem.TrySendInGameICMessage(entity, message, InGameICChatType.Speak, ChatTransmitRange.Normal);
        _radioSystem.SendRadioMessage(entity, message, entity.Comp.RadioChannel, entity);
    }
}
