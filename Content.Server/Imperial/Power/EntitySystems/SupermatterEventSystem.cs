using Content.Server.Atmos.EntitySystems;
using Content.Server.Chat.Systems;
using Content.Server.Imperial.Power.Components;
using Content.Server.Imperial.Power.EntitySystems.Events;
using Content.Server.Radio.EntitySystems;
using Content.Server.Lightning;
using Content.Shared.Damage;
using Content.Shared.Radiation.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Server.NukeOps;
using Content.Shared.NukeOps;

namespace Content.Server.Imperial.Power.EntitySystems
{
    public sealed class SupermatterEventSystem : EntitySystem
    {
        [Dependency] public readonly AtmosphereSystem Atmos = default!;
        [Dependency] private readonly RadioSystem _radio = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] public readonly SharedMapSystem MapSystem = default!;
        [Dependency] public readonly LightningSystem LightningSystem = default!;
        [Dependency] private readonly TransformSystem _transformSystem = default!;
        [Dependency] public readonly DamageableSystem Damageable = default!;
        [Dependency] public readonly IGameTiming GameTiming = default!;
        [Dependency] private readonly ChatSystem _chatSystem = default!;

        // Кеш ближайших консолей для кристаллов
        private readonly Dictionary<EntityUid, (EntityUid console, float time)> _nearestConsoleCache = new();

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SupermatterEventComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<SupermatterEventComponent, SupermatterTouchedEvent>(OnTouched);
            SubscribeLocalEvent<WarDeclaredEvent>(OnWarOps);
        }

        private void OnInit(EntityUid uid, SupermatterEventComponent comp, ComponentInit args)
        {
            var currentTime = GameTiming.CurTime;
            comp.LastConsoleCacheUpdate = currentTime;
            comp.LastEventEndTimeUpdate = currentTime;
            comp.LastNextEventTimerUpdate = currentTime;
            comp.LastLightningCooldownUpdate = currentTime;
            comp.LastPlasmaTickUpdate = currentTime;
        }

        private void OnWarOps(ref WarDeclaredEvent args)
        {
            if (args.Status != WarConditionStatus.YesWar)
                return;

            var componentEnumerator = EntityQueryEnumerator<SupermatterEventComponent>();
            while (componentEnumerator.MoveNext(out var uid, out var supermatterEventComponent))
            {
                supermatterEventComponent.IsWarOps = true;
                Log.Log(LogLevel.Info, $"Суперматерия ({uid}) более не может создавать события, так как статус войны теперь - {args.Status}");
            }
        }

        private void OnTouched(EntityUid uid, SupermatterEventComponent comp, SupermatterTouchedEvent args)
        {
            TriggerEventNow(uid);
        }

        private void TriggerEventNow(EntityUid uid)
        {
            if (!EntityManager.TryGetComponent<SupermatterEventComponent>(uid, out var comp))
                return;
            comp.EventEndTime = TimeSpan.Zero;
            comp.NextEventTimer = TimeSpan.Zero;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            var currentTime = GameTiming.CurTime;
            var enumerator = EntityQueryEnumerator<SupermatterEventComponent, TransformComponent>();

            while (enumerator.MoveNext(out var uid, out var comp, out _))
            {
                // Очистка кэша консоли
                if (currentTime - comp.LastConsoleCacheUpdate >= comp.ConsoleCacheLifetime)
                {
                    _nearestConsoleCache.Clear();
                    comp.LastConsoleCacheUpdate = currentTime;
                }

                var lastEvent = comp.CurrentEvent;

                // Обновление времени окончания события
                if (comp.EventEndTime > TimeSpan.Zero)
                {
                    var elapsedSinceLastUpdate = currentTime - comp.LastEventEndTimeUpdate;
                    comp.EventEndTime -= elapsedSinceLastUpdate;
                    if (comp.EventEndTime < TimeSpan.Zero)
                        comp.EventEndTime = TimeSpan.Zero;
                    comp.LastEventEndTimeUpdate = currentTime;
                }

                // Обновление таймера до следующего события
                if (comp.NextEventTimer > TimeSpan.Zero)
                {
                    var elapsedSinceLastUpdate = currentTime - comp.LastNextEventTimerUpdate;
                    comp.NextEventTimer -= elapsedSinceLastUpdate;
                    if (comp.NextEventTimer < TimeSpan.Zero)
                        comp.NextEventTimer = TimeSpan.Zero;
                    comp.LastNextEventTimerUpdate = currentTime;
                }

                if (comp.EventEndTime == TimeSpan.Zero
                    && comp.NextEventTimer == TimeSpan.Zero && !comp.IsWarOps)
                {
                    if (lastEvent == SupermatterEventComponent.SupermatterEventType.Radiation)
                    {
                        var rad = Comp<RadiationSourceComponent>(uid);
                        rad.Intensity = comp.DefaultRadiationIntensity;
                    }

                    var randomEvtIndex = _random.Next(0, comp.AllowedEventTypes.Count);
                    var randomEvtType = comp.AllowedEventTypes[randomEvtIndex];

                    comp.SupermatterEventTypesToEvents.TryGetValue(randomEvtType, out var eventHandler);

                    switch (eventHandler)
                    {
                        case SupermatterNoneEvent:
                            SupermatterNoneEvent.Activate(uid, comp, this);
                            AnnounceFromSupermatterConsole(uid, SupermatterNoneEvent.GetAnnouncement());
                            break;
                        case SupermatterLightningEvent:
                            SupermatterLightningEvent.Activate(uid, comp, this);
                            AnnounceFromSupermatterConsole(uid, SupermatterLightningEvent.GetAnnouncement());
                            break;
                        case SupermatterRadiationEvent:
                            SupermatterRadiationEvent.Activate(uid, comp, this);
                            AnnounceFromSupermatterConsole(uid, SupermatterRadiationEvent.GetAnnouncement());
                            break;
                        case SupermatterPlasmaEvent:
                            SupermatterPlasmaEvent.Activate(uid, comp, this);
                            AnnounceFromSupermatterConsole(uid, SupermatterPlasmaEvent.GetAnnouncement());
                            break;
                    }
                }

                if (comp.EventEndTime == TimeSpan.Zero)
                    continue;

                if (comp.SupermatterEventTypesToEvents.TryGetValue(comp.CurrentEvent, out var newEventHandler))
                {
                    switch (newEventHandler)
                    {
                        case SupermatterNoneEvent:
                            SupermatterNoneEvent.Process(uid, comp, this, currentTime);
                            break;
                        case SupermatterLightningEvent:
                            SupermatterLightningEvent.Process(uid, comp, this, currentTime);
                            break;
                        case SupermatterRadiationEvent:
                            SupermatterRadiationEvent.Process(uid, comp, this, currentTime);
                            break;
                        case SupermatterPlasmaEvent:
                            SupermatterPlasmaEvent.Process(uid, comp, this, currentTime);
                            break;
                    }
                }
            }
        }

        private void AnnounceFromSupermatterConsole(EntityUid crystal, string message)
        {
            var timeNow = (float)GameTiming.CurTime.TotalSeconds;
            EntityUid? nearestConsole = null;

            var mapCoordinates = _transformSystem.GetMapCoordinates(crystal);
            var crystalPos = mapCoordinates.Position;
            var mapId = mapCoordinates.MapId;

            if (!EntityManager.TryGetComponent<SupermatterEventComponent>(crystal, out var eventComp))
                return;

            if (_nearestConsoleCache.TryGetValue(crystal, out var cached) && TimeSpan.FromSeconds(timeNow - cached.time) < eventComp.ConsoleCacheLifetime)
            {
                nearestConsole = cached.console;
            }
            else
            {
                var minDist = float.MaxValue;
                var enumerator = EntityQueryEnumerator<SupermatterMonitorConsoleComponent, TransformComponent>();
                while (enumerator.MoveNext(out var consoleUid, out _, out var transformComp))
                {
                    if (transformComp.MapID != mapId)
                        continue;
                    var consolePos = _transformSystem.GetMapCoordinates(consoleUid).Position;
                    var dist = (consolePos - crystalPos).LengthSquared();

                    if (dist > minDist)
                        continue;

                    minDist = dist;
                    nearestConsole = consoleUid;
                }
                if (nearestConsole != null)
                    _nearestConsoleCache[crystal] = (nearestConsole.Value, timeNow);
            }

            foreach (var channel in eventComp.RadioChannels)
            {
                _chatSystem.TrySendInGameICMessage(nearestConsole ?? crystal, message, InGameICChatType.Speak, ChatTransmitRange.Normal);
                _radio.SendRadioMessage(nearestConsole ?? crystal, message, channel, nearestConsole ?? crystal);
            }
        }

        public void SetRadiation(EntityUid uid, float intensity)
        {
            if (EntityManager.TryGetComponent<RadiationSourceComponent>(uid, out var radComponent))
                radComponent.Intensity = intensity;
            else
            {
                var newRad = EntityManager.EnsureComponent<RadiationSourceComponent>(uid);
                newRad.Intensity = intensity;
            }
        }

        public bool TryGetComponent<T>(EntityUid uid, out T? component) where T : IComponent
        {
            return EntityManager.TryGetComponent(uid, out component);
        }
    }
}
