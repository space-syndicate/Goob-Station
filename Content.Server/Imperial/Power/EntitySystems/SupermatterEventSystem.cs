using Content.Server.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Chat.Managers;
using Content.Server.Imperial.Power.Components;
using Content.Server.Imperial.Power.EntitySystems.Events;
using Content.Server.Radio.EntitySystems;
using Content.Server.Radiation.Components;
using Content.Server.Radiation.Systems;
using Content.Server.Lightning;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Radiation.Components;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Content.Server.Imperial.Power.EntitySystems
{
    public sealed class SupermatterEventSystem : EntitySystem
    {
        [Dependency] public readonly AtmosphereSystem Atmos = default!;
        [Dependency] public readonly RadioSystem Radio = default!;
        [Dependency] public readonly IRobustRandom Random = default!;
        [Dependency] public readonly SharedMapSystem MapSystem = default!;
        [Dependency] public readonly LightningSystem LightningSystem = default!;
        [Dependency] public readonly TransformSystem TransformSystem = default!;
        [Dependency] public readonly DamageableSystem Damageable = default!;
        [Dependency] public readonly IGameTiming GameTiming = default!;

        // Словарь событий для быстрого доступа
        private readonly Dictionary<SupermatterEventType, object> _events = new()
        {
            { SupermatterEventType.None, new SupermatterNoneEvent() },
            { SupermatterEventType.Lightning, new SupermatterLightningEvent() },
            { SupermatterEventType.Radiation, new SupermatterRadiationEvent() },
            { SupermatterEventType.Plasma, new SupermatterPlasmaEvent() }
        };

        // Кеш ближайших консолей для кристаллов
        private readonly Dictionary<EntityUid, (EntityUid console, float time)> _nearestConsoleCache = new();

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SupermatterEventComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<SupermatterEventComponent, SupermatterTouchedEvent>(OnTouched);
        }

        private void OnInit(EntityUid uid, SupermatterEventComponent comp, ComponentInit args)
        {
            var currentTime = GameTiming.CurTime;
            comp.NextEventTimer = TimeSpan.FromSeconds(comp.InitialEventDelaySeconds); // 15 минут до первого ивента
            comp.LastConsoleCacheUpdate = currentTime;
            comp.LastEventEndTimeUpdate = currentTime;
            comp.LastNextEventTimerUpdate = currentTime;
            comp.LastLightningCooldownUpdate = currentTime;
            comp.LastPlasmaTickUpdate = currentTime;
        }

        private void OnTouched(EntityUid uid, SupermatterEventComponent comp, SupermatterTouchedEvent args)
        {
            TriggerEventNow(uid);
        }

        public void TriggerEventNow(EntityUid uid)
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
            while (enumerator.MoveNext(out var uid, out var comp, out var xform))
            {
                // Очистка кэша консоли
                if (currentTime - comp.LastConsoleCacheUpdate >= TimeSpan.FromSeconds(comp.ConsoleCacheLifetime))
                {
                    _nearestConsoleCache.Clear();
                    comp.LastConsoleCacheUpdate = currentTime;
                }

                var gas = Atmos.GetContainingMixture((uid, xform), true, false);
                var lastEvent = comp.CurrentEvent;

                // Обновление времени окончания события
                if (comp.EventEndTime > TimeSpan.Zero)
                {
                    var elapsedSinceLastUpdate = currentTime - comp.LastEventEndTimeUpdate;
                    comp.EventEndTime -= elapsedSinceLastUpdate;
                    comp.LastEventEndTimeUpdate = currentTime;
                }

                // Обновление таймера до следующего события
                if (comp.NextEventTimer > TimeSpan.Zero)
                {
                    var elapsedSinceLastUpdate = currentTime - comp.LastNextEventTimerUpdate;
                    comp.NextEventTimer -= elapsedSinceLastUpdate;
                    comp.LastNextEventTimerUpdate = currentTime;
                }

                if (comp.EventEndTime <= TimeSpan.Zero && comp.NextEventTimer <= TimeSpan.Zero)
                {
                    if (lastEvent == SupermatterEventType.Radiation && HasComp<RadiationSourceComponent>(uid))
                    {
                        var rad = Comp<RadiationSourceComponent>(uid);
                        rad.Intensity = comp.DefaultRadiationIntensity;
                    }
                    if (comp.AllowedEventTypes.Count == 0)
                        continue;
                    var evtIndex = Random.Next(0, comp.AllowedEventTypes.Count);
                    var evtType = comp.AllowedEventTypes[evtIndex];

                    if (_events.TryGetValue(evtType, out var eventHandler))
                    {
                        switch (eventHandler)
                        {
                            case SupermatterNoneEvent noneEvent:
                                noneEvent.Activate(uid, comp, this);
                                AnnounceFromConsole(uid, noneEvent.GetAnnouncement());
                                break;
                            case SupermatterLightningEvent lightningEvent:
                                lightningEvent.Activate(uid, comp, this);
                                AnnounceFromConsole(uid, lightningEvent.GetAnnouncement());
                                break;
                            case SupermatterRadiationEvent radiationEvent:
                                radiationEvent.Activate(uid, comp, this);
                                AnnounceFromConsole(uid, radiationEvent.GetAnnouncement());
                                break;
                            case SupermatterPlasmaEvent plasmaEvent:
                                plasmaEvent.Activate(uid, comp, this);
                                AnnounceFromConsole(uid, plasmaEvent.GetAnnouncement());
                                break;
                        }
                    }
                }
                if (comp.EventEndTime > TimeSpan.Zero)
                {
                    if (_events.TryGetValue(comp.CurrentEvent, out var eventHandler))
                    {
                        switch (eventHandler)
                        {
                            case SupermatterNoneEvent noneEvent:
                                noneEvent.Process(uid, comp, this, currentTime);
                                break;
                            case SupermatterLightningEvent lightningEvent:
                                lightningEvent.Process(uid, comp, this, currentTime);
                                break;
                            case SupermatterRadiationEvent radiationEvent:
                                radiationEvent.Process(uid, comp, this, currentTime);
                                break;
                            case SupermatterPlasmaEvent plasmaEvent:
                                plasmaEvent.Process(uid, comp, this, currentTime);
                                break;
                        }
                    }
                }
            }
        }

        private void AnnounceFromConsole(EntityUid crystal, string message)
        {
            var now = (float)GameTiming.CurTime.TotalSeconds;
            EntityUid? nearestConsole = null;
            var pos = TransformSystem.GetMapCoordinates(crystal).Position;
            var mapId = TransformSystem.GetMapCoordinates(crystal).MapId;
            if (EntityManager.TryGetComponent<SupermatterEventComponent>(crystal, out var eventComp))
            {
                if (_nearestConsoleCache.TryGetValue(crystal, out var cached) && now - cached.time < eventComp.ConsoleCacheLifetime)
                {
                    nearestConsole = cached.console;
                }
                else
                {
                    var minDist = float.MaxValue;
                    var enumerator = EntityQueryEnumerator<SupermatterMonitorConsoleComponent, TransformComponent>();
                    while (enumerator.MoveNext(out var consoleUid, out var cComp, out var cXform))
                    {
                        if (cXform.MapID != mapId)
                            continue;
                        var cPos = TransformSystem.GetMapCoordinates(consoleUid).Position;
                        var dist = (cPos - pos).LengthSquared();
                        if (dist < minDist)
                        {
                            minDist = dist;
                            nearestConsole = consoleUid;
                        }
                    }
                    if (nearestConsole != null)
                        _nearestConsoleCache[crystal] = (nearestConsole.Value, now);
                }

                foreach (var channel in eventComp.RadioChannels)
                {
                    Radio.SendRadioMessage(nearestConsole ?? crystal, message, channel, nearestConsole ?? crystal);
                }
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
