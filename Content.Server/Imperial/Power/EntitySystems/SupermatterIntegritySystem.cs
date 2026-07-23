using Content.Server.AlertLevel;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Chat.Systems;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Lightning;
using Content.Server.Station.Systems;
using Content.Shared.Audio;
using Content.Shared.Imperial.Power.Components;
using Content.Shared.Radiation.Components;
using System.Linq;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;
using Content.Server.Radiation.Systems;
using Content.Shared.Explosion.Components;
using Content.Shared.Imperial.Power.Events;

namespace Content.Server.Imperial.Power.EntitySystems;

public sealed class SupermatterIntegritySystem : EntitySystem
{
    [Dependency] private readonly AlertLevelSystem _alertLevelSystem = null!;
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = null!;
    [Dependency] private readonly ChatSystem _chatSystem = null!;
    [Dependency] private readonly ExplosionSystem _explosionSystem = null!;
    [Dependency] private readonly IGameTiming _gameTiming = null!;
    [Dependency] private readonly LightningSystem _lightning = null!;
    [Dependency] private readonly RadiationSystem _radiationSystem = null!;
    [Dependency] private readonly SharedAmbientSoundSystem _ambientSound = null!;
    [Dependency] private readonly SharedPointLightSystem _lightSystem = null!;
    [Dependency] private readonly StationSystem _stationSystem = null!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var enumerator = EntityQueryEnumerator<SupermatterIntegrityComponent, TransformComponent>();
        while (enumerator.MoveNext(out var uid, out var comp, out var transComp))
        {
            ProcessSupermatterUpdate((uid, comp), transComp);
        }
    }

    private void ProcessSupermatterUpdate(Entity<SupermatterIntegrityComponent> ent, TransformComponent transComp)
    {
        if (TryComp(ent, out RadiationSourceComponent? radiation))
            _radiationSystem.SetSourceEnabled((ent, radiation), ent.Comp.Activated);

        if (TryComp(ent, out PointLightComponent? light))
            _lightSystem.SetEnabled(ent, ent.Comp.Activated, light);

        if (TryComp(ent, out AmbientSoundComponent? ambient))
        {
            if (ent.Comp.Activated)
            {
                _ambientSound.SetVolume(ent, ent.Comp.AmbientSound[0].Volume, ambient);
                _ambientSound.SetRange(ent, ent.Comp.AmbientSound[0].Range, ambient);
            }
            else
            {
                _ambientSound.SetVolume(ent, ent.Comp.AmbientSound[1].Volume, ambient);
                _ambientSound.SetRange(ent, ent.Comp.AmbientSound[1].Range, ambient);
            }
        }

        if (!ent.Comp.Activated)
            return;

        var gas = _atmosphereSystem.GetContainingMixture((ent, transComp), true, true);

        bool badConditions;
        if (gas != null)
        {
            badConditions = gas.Temperature < ent.Comp.TempThresholds.Item1
                            || gas.Temperature > ent.Comp.TempThresholds.Item2
                            || gas.Pressure < ent.Comp.PressureThresholds.Item1
                            || gas.Pressure > ent.Comp.PressureThresholds.Item2;
        }
        else
            badConditions = true;

        var integrityPercent = ent.Comp.Integrity / ent.Comp.MaxIntegrity * 100f;

        // Сброс флага предупреждения для текущего уровня
        var index = ent.Comp.SupermatterIntegrity.FindIndex(entry => integrityPercent > entry.Threshold);
        if (index >= 0)
        {
            var oldEntry = ent.Comp.SupermatterIntegrity[index];
            if (oldEntry.Flag)
                oldEntry.Flag = false;
        }

        foreach (var level in ent.Comp.SupermatterIntegrity.OrderByDescending(entry => entry.Threshold))
        {
            if (integrityPercent > level.Threshold || level.Flag || string.IsNullOrEmpty(level.Warning))
                continue;

            var integrityWarning = Loc.GetString(level.Warning);
            var ev = new SupermatterSendRadioEvent(integrityWarning);
            RaiseLocalEvent(ent, ref ev);

            // Если мы достигли уровня с порогом <= 10% — выставляем код тревоги для станции и объявление.
            // Раньше использовался MinBy по всем порогам (что возвращало 0) и из-за этого код не ставился.
            if (level.Threshold <= 30f)
            {
                var station = _stationSystem.GetOwningStation(ent, transComp);
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
        if (!ent.Comp.CatastropheActivated && integrityPercent <= ent.Comp.CatastropheThreshold)
        {
            ent.Comp.CatastropheActivated = true;
            ent.Comp.CatastropheEndTime = _gameTiming.CurTime + ent.Comp.CatastropheDuration;
            ent.Comp.CatastropheLightningNextTime = _gameTiming.CurTime + ent.Comp.CatastropheLightningInterval;

            // Отправляем предупреждение о катастрофе
            var station = _stationSystem.GetOwningStation(ent, transComp);
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

        if (ent.Comp.CatastropheActivated)
        {
            // Молнии во время катастрофы
            if (_gameTiming.CurTime > ent.Comp.CatastropheLightningNextTime)
            {
                ent.Comp.CatastropheLightningNextTime = _gameTiming.CurTime + ent.Comp.CatastropheLightningInterval;
                _lightning.ShootRandomLightnings(ent, ent.Comp.CatastropheLightningRange, ent.Comp.CatastropheLightningCount);
            }

            if (_gameTiming.CurTime > ent.Comp.CatastropheEndTime)
            {
                if (TryComp<ExplosiveComponent>(ent, out var explosive))
                    _explosionSystem.TriggerExplosive(ent, explosive);
                else
                    QueueDel(ent);
                return;
            }
        }

        // Обработка урона от плохих условий
        if (badConditions)
        {
            if (ent.Comp.NextDamageTime == TimeSpan.MaxValue)
                ent.Comp.NextDamageTime = _gameTiming.CurTime + ent.Comp.DamageInterval;

            if (_gameTiming.CurTime < ent.Comp.NextDamageTime)
                return;

            ent.Comp.NextDamageTime = _gameTiming.CurTime + ent.Comp.DamageInterval;

            var tickAmount = ent.Comp.TickDamage.GetTotal().Float();
            if (tickAmount <= 0)
                tickAmount = 0.65f;

            ent.Comp.Integrity = MathF.Max(0, ent.Comp.Integrity - tickAmount);
            Dirty(ent);
        }
        else
            ent.Comp.NextDamageTime = TimeSpan.MaxValue;
    }
}
