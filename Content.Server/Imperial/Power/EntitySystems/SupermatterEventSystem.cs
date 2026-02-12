using Content.Server.Atmos.EntitySystems;
using Content.Server.Chat.Systems;
using Content.Server.Imperial.Power.Components;
using Content.Server.Lightning;
using Content.Server.NukeOps;
using Content.Server.Radio.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Chat;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.NukeOps;
using Content.Shared.Radiation.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Imperial.Power.EntitySystems;

public sealed class SupermatterEventSystem : EntitySystem
{
    [Dependency] public readonly AtmosphereSystem Atmos = null!;
    [Dependency] public readonly DamageableSystem Damageable = null!;
    [Dependency] public readonly IGameTiming GameTiming = null!;
    [Dependency] public readonly LightningSystem LightningSystem = null!;
    [Dependency] public readonly SharedMapSystem MapSystem = null!;
    [Dependency] private readonly ChatSystem _chatSystem = null!;
    [Dependency] private readonly IRobustRandom _random = null!;
    [Dependency] private readonly RadioSystem _radio = null!;
    [Dependency] private readonly TransformSystem _transformSystem = null!;

    // Кеш ближайших консолей для кристаллов
    private readonly Dictionary<EntityUid, (EntityUid console, float time)> _nearestConsoleCache = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SupermatterEventComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<SupermatterEventComponent, SupermatterTouchedEvent>(OnTouched);
        SubscribeLocalEvent<WarDeclaredEvent>(OnWarOps);
    }

    private void OnInit(Entity<SupermatterEventComponent> entity, ref ComponentInit args)
    {
        var currentTime = GameTiming.CurTime;
        entity.Comp.LastConsoleCacheUpdate = currentTime;
        entity.Comp.LastEventEndTimeUpdate = currentTime;
        entity.Comp.LastNextEventTimerUpdate = currentTime;
        entity.Comp.LastLightningCooldownUpdate = currentTime;
        entity.Comp.LastPlasmaTickUpdate = currentTime;
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

    private void OnTouched(Entity<SupermatterEventComponent> entity, ref SupermatterTouchedEvent args)
    {
        TriggerEventNow(entity);
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

        var enumerator = EntityQueryEnumerator<SupermatterEventComponent, SupermatterIntegrityComponent>();

        while (enumerator.MoveNext(out var uid, out var eventComp, out var integrityComp))
        {
            Entity<SupermatterEventComponent, SupermatterIntegrityComponent> entity = new(uid, eventComp, integrityComp);
            
            var currentTime = GameTiming.CurTime;
            
            // Очистка кэша консоли
            if (currentTime - eventComp.LastConsoleCacheUpdate >= eventComp.ConsoleCacheLifetime)
            {
                _nearestConsoleCache.Remove(uid);
                eventComp.LastConsoleCacheUpdate = currentTime;
            }

            UpdateEventEndTimer(eventComp, currentTime);

            if (!integrityComp.Activated)
            {
                ResetInactiveTimers(eventComp, currentTime);
                continue;
            }

            if (eventComp.NextEventTimer > TimeSpan.Zero)
            {
                var elapsedSinceLastUpdate = currentTime - eventComp.LastNextEventTimerUpdate;
                eventComp.NextEventTimer -= elapsedSinceLastUpdate;

                if (EntityManager.TryGetComponent<SupermatterGasComponent>(uid, out var gasComp) &&
                    gasComp.WaterVaporEventSpeedMultiplier > 1f)
                {
                    var extra = TimeSpan.FromTicks((long)(elapsedSinceLastUpdate.Ticks * (gasComp.WaterVaporEventSpeedMultiplier - 1f)));
                    eventComp.NextEventTimer -= extra;
                }

                if (eventComp.NextEventTimer < TimeSpan.Zero)
                    eventComp.NextEventTimer = TimeSpan.Zero;
                eventComp.LastNextEventTimerUpdate = currentTime;
            }

            TryStartNewEvent(entity);
            ProcessActiveEvent(entity, currentTime);
        }
    }

    private void ProcessSingleSupermatter(
        Entity<SupermatterEventComponent, SupermatterIntegrityComponent> entity)
    {
        var currentTime = GameTiming.CurTime;

        UpdateConsoleCache(entity, currentTime);
        UpdateEventEndTimer(entity.Comp1, currentTime);

        if (!entity.Comp2.Activated)
        {
            ResetInactiveTimers(entity.Comp1, currentTime);
            return;
        }

        UpdateNextEventTimer(entity.Comp1, currentTime);

        TryStartNewEvent(entity);
        ProcessActiveEvent(entity, currentTime);
    }


    private void UpdateConsoleCache(Entity<SupermatterEventComponent> entity, TimeSpan currentTime)
    {
        if (currentTime - entity.Comp.LastConsoleCacheUpdate < entity.Comp.ConsoleCacheLifetime)
            return;

        _nearestConsoleCache.Remove(entity);
        entity.Comp.LastConsoleCacheUpdate = currentTime;
    }

    private static void UpdateEventEndTimer(SupermatterEventComponent comp, TimeSpan currentTime)
    {
        if (comp.EventEndTime <= TimeSpan.Zero)
            return;

        var elapsed = currentTime - comp.LastEventEndTimeUpdate;
        comp.EventEndTime -= elapsed;
        if (comp.EventEndTime < TimeSpan.Zero)
            comp.EventEndTime = TimeSpan.Zero;

        comp.LastEventEndTimeUpdate = currentTime;
    }

    private static void UpdateNextEventTimer(SupermatterEventComponent comp, TimeSpan currentTime)
    {
        if (comp.NextEventTimer <= TimeSpan.Zero)
            return;

        var elapsed = currentTime - comp.LastNextEventTimerUpdate;
        comp.NextEventTimer -= elapsed;
        if (comp.NextEventTimer < TimeSpan.Zero)
            comp.NextEventTimer = TimeSpan.Zero;

        comp.LastNextEventTimerUpdate = currentTime;
    }

    private static void ResetInactiveTimers(SupermatterEventComponent comp, TimeSpan currentTime)
    {
        comp.LastNextEventTimerUpdate = currentTime;
        comp.LastEventEndTimeUpdate = currentTime;
    }

    private void TryStartNewEvent(Entity<SupermatterEventComponent> entity)
    {
        var comp = entity.Comp;

        if (comp.EventEndTime > TimeSpan.Zero
            || comp.NextEventTimer > TimeSpan.Zero
            || comp.IsWarOps)
            return;

        if (comp.CurrentEvent == SupermatterEventComponent.SupermatterEventType.Radiation
            && TryComp<RadiationSourceComponent>(entity.Owner, out var rad))
        {
            rad.Intensity = comp.DefaultRadiationIntensity;
        }

        var randomEvtIndex = _random.Next(0, comp.AllowedEventTypes.Count);
        var randomEvtType = comp.AllowedEventTypes[randomEvtIndex];

        switch (randomEvtType)
        {
            case SupermatterEventComponent.SupermatterEventType.None:
                ActivateNoneEvent(entity);
                AnnounceFromSupermatterConsole(entity.Owner, GetNoneAnnouncement());
                break;
            case SupermatterEventComponent.SupermatterEventType.Lightning:
                ActivateLightningEvent(entity);
                AnnounceFromSupermatterConsole(entity.Owner, GetLightningAnnouncement());
                break;
            case SupermatterEventComponent.SupermatterEventType.Radiation:
                ActivateRadiationEvent(entity);
                AnnounceFromSupermatterConsole(entity.Owner, GetRadiationAnnouncement());
                break;
            case SupermatterEventComponent.SupermatterEventType.Plasma:
                ActivatePlasmaEvent(entity);
                AnnounceFromSupermatterConsole(entity.Owner, GetPlasmaAnnouncement());
                break;
        }
    }

    private void ProcessActiveEvent(Entity<SupermatterEventComponent> entity, TimeSpan currentTime)
    {
        var comp = entity.Comp;

        if (comp.EventEndTime == TimeSpan.Zero)
            return;

        switch (comp.CurrentEvent)
        {
            case SupermatterEventComponent.SupermatterEventType.None:
                ProcessNoneEvent(entity, currentTime);
                break;
            case SupermatterEventComponent.SupermatterEventType.Lightning:
                ProcessLightningEvent(entity, currentTime);
                break;
            case SupermatterEventComponent.SupermatterEventType.Radiation:
                ProcessRadiationEvent(entity, currentTime);
                break;
            case SupermatterEventComponent.SupermatterEventType.Plasma:
                ProcessPlasmaEvent(entity, currentTime);
                break;
        }
    }

    private static void ActivateNoneEvent(Entity<SupermatterEventComponent> entity)
    {
        if (entity.AsType() == EntityUid.Invalid)
            return;

        var comp = entity.Comp;
        var currentTime = comp.LastEventEndTimeUpdate;

        comp.CurrentEvent = SupermatterEventComponent.SupermatterEventType.None;
        comp.EventEndTime = TimeSpan.Zero;
        comp.NextEventTimer = comp.NoneEventDuration;
        comp.LastEventEndTimeUpdate = currentTime;
        comp.LastNextEventTimerUpdate = currentTime;
    }

    private static void ProcessNoneEvent(Entity<SupermatterEventComponent> entity, TimeSpan _)
    {
    }

    private static string GetNoneAnnouncement()
    {
        return Robust.Shared.Localization.Loc.GetString("supermatter-event-none");
    }

    private void ActivateLightningEvent(Entity<SupermatterEventComponent> entity)
    {
        if (entity.AsType() == EntityUid.Invalid)
        {
            Log.Error("SupermatterEventSystem.ActivateLightningEvent: Invalid EntityUid provided");
            return;
        }

        var comp = entity.Comp;
        var currentTime = GameTiming.CurTime;

        comp.CurrentEvent = SupermatterEventComponent.SupermatterEventType.Lightning;
        comp.EventEndTime = comp.LightningEventDuration;
        comp.NextEventTimer = comp.EventAfterLightingTime;
        comp.LightningCooldown = TimeSpan.Zero;
        comp.LastEventEndTimeUpdate = currentTime;
        comp.LastNextEventTimerUpdate = currentTime;
        comp.LastLightningCooldownUpdate = currentTime;

        ShootRandomLightnings(entity);
    }

    private void ProcessLightningEvent(Entity<SupermatterEventComponent> entity, TimeSpan currentTime)
    {
        var comp = entity.Comp;

        var elapsedSinceLastUpdate = currentTime - comp.LastLightningCooldownUpdate;
        comp.LightningCooldown -= elapsedSinceLastUpdate;
        comp.LastLightningCooldownUpdate = currentTime;

        if (comp.LightningCooldown > TimeSpan.Zero)
            return;

        ShootRandomLightnings(entity);

        if (TryGetComponent<SupermatterIntegrityComponent>(entity, out var integrity) &&
            integrity != null &&
            TryGetComponent<DamageableComponent>(entity, out _))
        {
            Damageable.TryChangeDamage(entity.Owner, integrity.TickDamage, origin: null);
        }

        comp.LightningCooldown = comp.LightningCooldownDuration;
    }

    private void ShootRandomLightnings(Entity<SupermatterEventComponent> entity)
    {
        var boltCount = entity.Comp.LightningBoltCount;

        if (TryGetComponent<SupermatterGasComponent>(entity, out var gasComp)
            && gasComp != null
            && gasComp.CurrentLightningMultiplier > 1f)
        {
            boltCount = (int) MathF.Max(1, boltCount * gasComp.CurrentLightningMultiplier);
        }

        LightningSystem?.ShootRandomLightnings(entity, entity.Comp.LightningBoltRadius, boltCount);
    }

    private static string GetLightningAnnouncement()
    {
        return Robust.Shared.Localization.Loc.GetString("supermatter-event-lightning");
    }

    private void ActivateRadiationEvent(Entity<SupermatterEventComponent> entity)
    {
        if (entity.AsType() == EntityUid.Invalid)
        {
            Log.Error("SupermatterEventSystem.ActivateRadiationEvent: Invalid EntityUid provided");
            return;
        }

        var comp = entity.Comp;
        var currentTime = GameTiming.CurTime;

        comp.CurrentEvent = SupermatterEventComponent.SupermatterEventType.Radiation;
        comp.EventEndTime = comp.RadiationEventDuration;
        comp.NextEventTimer = comp.EventAfterRadiationTime;
        comp.LastEventEndTimeUpdate = currentTime;
        comp.LastNextEventTimerUpdate = currentTime;

        SetRadiation(entity.Owner, comp.RadiationEventIntensity);
    }

    private void ProcessRadiationEvent(Entity<SupermatterEventComponent> entity, TimeSpan _)
    {
    }

    private static string GetRadiationAnnouncement()
    {
        return Robust.Shared.Localization.Loc.GetString("supermatter-event-radiation");
    }

    private void ActivatePlasmaEvent(Entity<SupermatterEventComponent> entity)
    {
        if (entity.AsType() == EntityUid.Invalid)
        {
            Log.Error("SupermatterEventSystem.ActivatePlasmaEvent: Invalid EntityUid provided");
            return;
        }

        var comp = entity.Comp;
        var currentTime = GameTiming.CurTime;

        comp.CurrentEvent = SupermatterEventComponent.SupermatterEventType.Plasma;
        comp.EventEndTime = comp.PlasmaEventDuration;
        comp.NextEventTimer = comp.EventAfterPlasmaTime;
        comp.LastEventEndTimeUpdate = currentTime;
        comp.LastNextEventTimerUpdate = currentTime;
        comp.LastPlasmaTickUpdate = currentTime;
    }

    private void ProcessPlasmaEvent(Entity<SupermatterEventComponent> entity, TimeSpan currentTime)
    {
        var comp = entity.Comp;

        comp.PlasmaTickAccumulator ??= TimeSpan.Zero;

        var elapsedSinceLastUpdate = currentTime - comp.LastPlasmaTickUpdate;
        comp.PlasmaTickAccumulator += elapsedSinceLastUpdate;
        comp.LastPlasmaTickUpdate = currentTime;

        if (comp.PlasmaTickAccumulator < comp.PlasmaTickInterval)
            return;

        if (!TryGetComponent<TransformComponent>(entity, out var xform) || xform == null)
        {
            return;
        }

        var gas = Atmos.GetContainingMixture(entity.Owner, true);
        if (gas == null)
            return;

        gas.AdjustMoles((int) Gas.Plasma, comp.PlasmaMolesAmount);
        gas.AdjustMoles((int) Gas.Oxygen, comp.PlasmaMolesAmount);

        if (!TryGetGridUid(xform, out var gridUid))
        {
            Log.Warning($"Supermatter plasma event triggered for entity {entity} without grid");
            return;
        }

        if (!TryGetComponent<MapGridComponent>(gridUid, out var grid) || grid == null)
            return;

        var tile = MapSystem.TileIndicesFor(gridUid, grid, xform.Coordinates);
        CreateHotspot(Atmos, gridUid, tile, comp.PlasmaHotspotTemperature, comp.PlasmaHotspotVolume, entity);

        comp.PlasmaTickAccumulator -= comp.PlasmaTickInterval;
    }

    private static bool TryGetGridUid(TransformComponent xform, out EntityUid gridUid)
    {
        gridUid = xform.GridUid ?? default;
        return xform.GridUid.HasValue;
    }

    private static void CreateHotspot(AtmosphereSystem atmos, EntityUid gridUid, Vector2i tile, float temp, float volume, EntityUid uid)
    {
        atmos.HotspotExpose(gridUid, tile, temp, volume, uid, true);
    }

    private static string GetPlasmaAnnouncement()
    {
        return Robust.Shared.Localization.Loc.GetString("supermatter-event-plasma");
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
