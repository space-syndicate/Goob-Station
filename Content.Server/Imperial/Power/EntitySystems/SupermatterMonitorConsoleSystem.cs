using Content.Server.Imperial.Power.Components;
using Content.Shared.Examine;
using System.Linq;
using Content.Server.Atmos.EntitySystems;
using Robust.Shared.Random;
using Robust.Shared.Audio.Systems;

namespace Content.Server.Imperial.Power.EntitySystems
{
    public sealed class SupermatterMonitorConsoleSystem : EntitySystem
    {
        [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
        [Dependency] private readonly AtmosphereSystem _atmosSystem = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly SharedAudioSystem _audioSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SupermatterMonitorConsoleComponent, ExaminedEvent>(OnExamined);
        }

        private void OnExamined(EntityUid uid, SupermatterMonitorConsoleComponent component, ExaminedEvent args)
        {
            if (!args.IsInDetailsRange)
                return;

            var nearestUid = FindNearestSupermatter(uid);
            if (nearestUid == null || !TryComp(nearestUid, out SupermatterIntegrityComponent? nearest))
            {
                args.PushMarkup(Loc.GetString("supermatter-monitor-none-nearby"));
                return;
            }

            var (integrityPercent, level) = CalculateIntegrity(nearest);
            var integrity = (int)Math.Round(integrityPercent);

            args.PushMarkup(Loc.GetString("supermatter-monitor-integrity",
                ("integrity", integrity),
                ("color", level.Color)));

            var transComp = Transform(nearestUid.Value);
            var gas = _atmosSystem.GetContainingMixture((nearestUid.Value, transComp));
            if (gas != null)
            {
                var pressure = (int)Math.Round(gas.Pressure);
                var temperature = (int)Math.Round(gas.Temperature);
                args.PushMarkup(Loc.GetString("supermatter-monitor-atmospherics",
                    ("pressure", pressure),
                    ("temperature", temperature)));
            }

            if (!EntityManager.TryGetComponent<SupermatterEventComponent>(nearestUid.Value, out var events))
                return;

            var next = events.NextEventTimer.TotalSeconds;
            var approx = Math.Max(0, next + _random.Next(-60, 61));
            var minutes = (int)Math.Round(approx / 60.0);
            args.PushMarkup(Loc.GetString("supermatter-monitor-next-event", ("minutes", minutes)));
        }

        private EntityUid? FindNearestSupermatter(EntityUid consoleUid)
        {
            var transformCompConsole = Transform(consoleUid);
            var mapId = transformCompConsole.MapID;
            var pos = _transformSystem.GetMapCoordinates(transformCompConsole).Position;

            EntityUid? nearest = null;
            var minDist = float.MaxValue;

            var smEnumerator = EntityQueryEnumerator<SupermatterIntegrityComponent, TransformComponent>();
            while (smEnumerator.MoveNext(out var smUid, out _, out var transComp))
            {
                if (transComp.MapID != mapId)
                    continue;

                var smPos = _transformSystem.GetMapCoordinates(smUid).Position;
                var dist = (smPos - pos).LengthSquared();
                if (dist > minDist)
                    continue;

                minDist = dist;
                nearest = smUid;
            }
            return nearest;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var enumerator = EntityQueryEnumerator<SupermatterMonitorConsoleComponent, TransformComponent>();
            while (enumerator.MoveNext(out var uid, out var console, out _))
            {
                var nearestUid = FindNearestSupermatter(uid);
                if (nearestUid == null ||
                    !EntityManager.TryGetComponent<SupermatterIntegrityComponent>(nearestUid.Value, out var nearest))
                {
                    console.BeepCooldownTimer = TimeSpan.Zero;
                    continue;
                }

                var (integrityPercent, level) = CalculateIntegrity(nearest);
                var integrity = (int)Math.Round(integrityPercent);

                var highestThreshold = nearest.SupermatterIntegrity.MaxBy(e => e.Threshold).Threshold;
                if (integrity >= highestThreshold)
                {
                    console.BeepCooldownTimer = TimeSpan.Zero;
                }
                else
                {
                    console.BeepCooldownTimer -= TimeSpan.FromSeconds(frameTime);
                    if (console.BeepCooldownTimer > TimeSpan.Zero)
                        continue;

                    _audioSystem.PlayPvs(console.BeepSound, uid);
                    console.BeepCooldownTimer = TimeSpan.FromSeconds(2);
                }
            }
        }

        private static (float integrity, (float Threshold, Color Color, LocId Description, LocId Warning, bool Flag) integrityLevel)
            CalculateIntegrity(SupermatterIntegrityComponent component)
        {
            var integrity = component.Integrity / component.MaxIntegrity * 100f;

            // Выбираем самый высокий уровень, порог которого меньше или равен текущей целостности.
            var ordered = component.SupermatterIntegrity.OrderByDescending(e => e.Threshold).ToList();
            var idx = ordered.FindIndex(entry => integrity >= entry.Threshold);
            (float Threshold, Color Color, LocId Description, LocId Warning, bool Flag) level;
            if (idx >= 0)
            {
                level = ordered[idx];
            }
            else
            {
                level = component.SupermatterIntegrity.MinBy(e => e.Threshold);
            }

            return (integrity, level);
        }
    }
}
