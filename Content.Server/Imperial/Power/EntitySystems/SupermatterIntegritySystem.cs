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
using Content.Shared.Imperial.Power;
using Robust.Shared.Physics.Events;
using System.Linq;
using Content.Server.DoAfter;
using Robust.Server.GameObjects;
using Content.Shared.Chat;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Robust.Server.Audio;

namespace Content.Server.Imperial.Power.EntitySystems;

public sealed class SupermatterIntegritySystem : EntitySystem
{
    [Dependency] private readonly AlertLevelSystem _alertLevelSystem = null!;
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = null!;
    [Dependency] private readonly AudioSystem _audioSystem = null!;
    [Dependency] private readonly ChatSystem _chatSystem = null!;
    [Dependency] private readonly DoAfterSystem _doAfterSystem = null!;
    [Dependency] private readonly ExplosionSystem _explosionSystem = null!;
    [Dependency] private readonly LightningSystem _lightning = null!;
    [Dependency] private readonly SharedAmbientSoundSystem _ambientSound = null!;
    [Dependency] private readonly SharedPointLightSystem _lightSystem = null!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = null!;
    [Dependency] private readonly RadioSystem _radioSystem = null!;
    [Dependency] private readonly StationSystem _stationSystem = null!;
    [Dependency] private readonly TagSystem _tagSystem = null!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SupermatterIntegrityComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<SupermatterIntegrityComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<SupermatterIntegrityComponent, AfterInteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<SupermatterIntegrityComponent, SupermatterShutdownDoAfterEvent>(OnDoAfter);
    }

    private void OnExamined(Entity<SupermatterIntegrityComponent> entity, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(entity.Comp.Activated
            ? $"[color=yellow]{Loc.GetString("supermatter-status-active")}[/color]"
            : $"[color=gray]{Loc.GetString("supermatter-status-inactive")}[/color]");

        var integrityPercent = entity.Comp.Integrity / entity.Comp.MaxIntegrity * 100;
        var integrityLevel = entity.Comp.SupermatterIntegrity.First(entry => integrityPercent > entry.Threshold);

        args.PushMarkup(Loc.GetString(integrityLevel.Description));
    }

    private void OnStartCollide(Entity<SupermatterIntegrityComponent> entity, ref StartCollideEvent args)
    {
        var other = args.OtherEntity;
        if (!_tagSystem.HasTag(other, entity.Comp.HealTag))
            return;

        if (!entity.Comp.Activated)
        {
            entity.Comp.Activated = true;
            SendSupermatterRadio(entity, Loc.GetString("supermatter-activated"));
        }

        entity.Comp.Integrity = MathF.Min(entity.Comp.MaxIntegrity, entity.Comp.Integrity + entity.Comp.EmitterHealAmount);
    }

    private void OnInteractUsing(Entity<SupermatterIntegrityComponent> entity, ref AfterInteractUsingEvent args)
    {
        if (!_tagSystem.HasTag(args.Used, entity.Comp.SupermatterStopTag)
            || args.Target == null)
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, 5, new SupermatterShutdownDoAfterEvent(), entity, args.Target, args.Used)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            BreakOnHandChange = true,
        };

        _doAfterSystem.TryStartDoAfter(doAfterArgs);
    }

    private void OnDoAfter(Entity<SupermatterIntegrityComponent> entity, ref SupermatterShutdownDoAfterEvent args)
    {
        if (!entity.Comp.Activated)
            return;

        _audioSystem.PlayPvs(entity.Comp.ShutdownSoundPath, entity);

        QueueDel(args.Used);
        entity.Comp.Activated = false;
        SendSupermatterRadio(entity, Loc.GetString("supermatter-deactivated"));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var enumerator = EntityQueryEnumerator<SupermatterIntegrityComponent, TransformComponent>();
        while (enumerator.MoveNext(out var uid, out var comp, out var transComp))
        {
            Entity<SupermatterIntegrityComponent> entity = new(uid, comp);
            ProcessSupermatterUpdate(entity, transComp, frameTime);
        }
    }

    private void ProcessSupermatterUpdate(Entity<SupermatterIntegrityComponent> entity, TransformComponent transComp, float frameTime)
    {
        if (TryComp(entity, out RadiationSourceComponent? radiation))
            radiation.Enabled = entity.Comp.Activated;

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
                entity.Comp.SupermatterIntegrity[index] = (oldEntry.Threshold, oldEntry.Color, oldEntry.Description, oldEntry.Warning, false);
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
            var levelIndex = entity.Comp.SupermatterIntegrity.FindIndex(entry => Math.Abs(level.Threshold - entry.Threshold) < 1f);
            if (levelIndex >= 0)
            {
                var updated = entity.Comp.SupermatterIntegrity[levelIndex];
                entity.Comp.SupermatterIntegrity[levelIndex] = (updated.Threshold, updated.Color, updated.Description, updated.Warning, true);
            }
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
                EntityManager.QueueDeleteEntity(entity);
                return;
            }
        }

        // Обработка урона от плохих условий
        if (badConditions)
        {
            entity.Comp.TickAccumulator += TimeSpan.FromSeconds(frameTime);
            while (entity.Comp.TickAccumulator >= entity.Comp.DamageTickInterval)
            {
                entity.Comp.TickAccumulator -= entity.Comp.DamageTickInterval;
                var tickAmount = entity.Comp.TickDamage.DamageDict.Values.Sum(v => (float)v);
                entity.Comp.Integrity = MathF.Max(0, entity.Comp.Integrity - tickAmount);
            }
        }
    }

    // Отправка сообщения в общую рацию от имени суперматерии
    private void SendSupermatterRadio(Entity<SupermatterIntegrityComponent> entity, string message)
    {
        _chatSystem.TrySendInGameICMessage(entity, message, InGameICChatType.Speak, ChatTransmitRange.Normal);
        _radioSystem.SendRadioMessage(entity, message, entity.Comp.RadioChannel, entity);
    }
}
