using Content.Server.Imperial.Power.Components;
using Content.Shared.Examine;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Radio.EntitySystems;
using Content.Shared.Tag;
using Robust.Shared.Physics.Events;
using Content.Server.Chat.Systems;
using Content.Server.AlertLevel;
using Content.Server.Station.Systems;
using Content.Server.Explosion.EntitySystems;
using System.Linq;

namespace Content.Server.Imperial.Power.EntitySystems
{
    public sealed class SupermatterIntegritySystem : EntitySystem
    {
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
        [Dependency] private readonly RadioSystem _radioSystem = default!;
        [Dependency] private readonly TagSystem _tagSystem = default!;
        [Dependency] private readonly ChatSystem _chatSystem = default!;
        [Dependency] private readonly StationSystem _stationSystem = default!;
        [Dependency] private readonly AlertLevelSystem _alertLevelSystem = default!;
        [Dependency] private readonly ExplosionSystem _explosionSystem = default!;
        [Dependency] private readonly SharedTransformSystem _transformSystem = default!;

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

            var integrityPercent = component.Integrity / component.MaxIntegrity * 100;
            var integrityLevel = component.SupermatterIntegrity.First(entry => integrityPercent > entry.Threshold);

            args.PushMarkup(Loc.GetString(integrityLevel.Description));
        }

        private void OnStartCollide(EntityUid uid, SupermatterIntegrityComponent component, ref StartCollideEvent args)
        {
            var other = args.OtherEntity;
            if (_tagSystem.HasTag(other, component.HealTag))
            {
                component.Integrity = MathF.Min(component.MaxIntegrity, component.Integrity + component.EmitterHealAmount);
            }
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
            var gas = _atmosphereSystem.GetContainingMixture((uid, transComp), true);

            var badConditions = false;
            if (gas != null)
            {
                if (gas.Temperature > comp.UpperTempThreshold ||
                    gas.Temperature < comp.LowerTempThreshold ||
                    gas.Pressure > comp.UpperPressureThreshold)
                {
                    badConditions = true;
                }
            }

            var integrityPercent = comp.Integrity / comp.MaxIntegrity * 100;

            var index = comp.SupermatterIntegrity.FindIndex(entry => integrityPercent > entry.Threshold);
            if (index >= 0)
            {
                // Replace with same tuple but Flag = false
                var oldEntry = comp.SupermatterIntegrity[index];
                comp.SupermatterIntegrity[index] = (oldEntry.Threshold, oldEntry.Color, oldEntry.Description, oldEntry.Warning, false);
            }

            foreach (var level in comp.SupermatterIntegrity.OrderByDescending(entry => entry.Threshold))
            {
                if (integrityPercent > level.Threshold
                    || level.Flag
                    || string.IsNullOrEmpty(level.Warning))
                {
                    continue;
                }

                var integrityWarning = Loc.GetString(level.Warning);

                SendSupermatterRadio(uid, integrityWarning, comp);

                var criticalThreshold = comp.SupermatterIntegrity.MinBy(entry => entry.Threshold);
                if (integrityPercent <= criticalThreshold.Threshold) // 10f
                {
                    var station = _stationSystem.GetOwningStation(uid);
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

                var levelIndex = comp.SupermatterIntegrity.FindIndex(entry => Math.Abs(level.Threshold - entry.Threshold) < 1f);
                var updated = comp.SupermatterIntegrity[levelIndex];

                comp.SupermatterIntegrity[levelIndex] = (updated.Threshold, updated.Color, updated.Description, updated.Warning, true);

                break;
            }

            // Обработка катастрофы
            if (!comp.CatastropheActive && comp.Integrity <= comp.CatastropheThreshold)
            {
                comp.CatastropheActive = true;
                comp.CatastropheTimer = TimeSpan.FromSeconds(120); // 2 минуты
            }

            if (comp.CatastropheActive)
            {
                comp.CatastropheTimer -= TimeSpan.FromSeconds(frameTime);
                if (comp.CatastropheTimer <= TimeSpan.Zero)
                {
                    // Взрыв
                    if (TryComp(uid, out TransformComponent? transCompCat))
                    {
                        var coords = _transformSystem.ToMapCoordinates(transCompCat.Coordinates);
                        _explosionSystem.QueueExplosion(
                            coords,
                            "Default", // TODO: Отдельный прототип взрыва
                            20000f,      // totalIntensity
                            1f,         // slope
                            70f,        // maxTileIntensity
                            cause: uid
                        );
                    }
                    EntityManager.QueueDeleteEntity(uid);

                    return;
                }
            }

            // Обработка урона от плохих условий
            if (!badConditions)
                return;

            comp.TickAccumulator += TimeSpan.FromSeconds(frameTime);
            while (comp.TickAccumulator >= comp.DamageTickInterval)
            {
                comp.TickAccumulator -= comp.DamageTickInterval;

                var tickAmount = 0f;
                foreach (var v in comp.TickDamage.DamageDict.Values)
                {
                    tickAmount += (float)v;
                }

                comp.Integrity = MathF.Max(0, comp.Integrity - tickAmount);
            }
        }

        // Отправка сообщения в общую рацию от имени суперматерии
        private void SendSupermatterRadio(EntityUid source, string message, SupermatterIntegrityComponent component)
        {
            _chatSystem.TrySendInGameICMessage(source, message, InGameICChatType.Speak, ChatTransmitRange.Normal);
            _radioSystem.SendRadioMessage(source, message, component.RadioChannel, source);
        }
    }
}
