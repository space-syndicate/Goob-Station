using Content.Server.Atmos.EntitySystems;
using Content.Server.Imperial.Power.Components;
using Content.Server.Imperial.Power.EntitySystems.Events;
using Content.Server.Lightning;
using Content.Server.NukeOps;
using Content.Server.Radiation.Systems;
using Content.Shared.Damage.Systems;
using Content.Shared.Imperial.Power.Components;
using Content.Shared.Imperial.Power.Events;
using Content.Shared.NukeOps;
using Content.Shared.Radiation.Components;
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
    [Dependency] private readonly IRobustRandom _random = null!;
    [Dependency] private readonly RadiationSystem _radiationSystem = null!;

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
        if (args.Cancelled)
            return;
        TriggerEventNow(entity);
    }

    private void TriggerEventNow(EntityUid uid)
    {
        if (!TryComp<SupermatterEventComponent>(uid, out var comp))
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
            ProcessSingleSupermatter(entity);
        }
    }

    private void ProcessSingleSupermatter(
        Entity<SupermatterEventComponent, SupermatterIntegrityComponent> entity)
    {
        var currentTime = GameTiming.CurTime;

        UpdateEventEndTimer(entity.Comp1, currentTime);

        if (!entity.Comp2.Activated)
        {
            ResetInactiveTimers(entity.Comp1, currentTime);
            return;
        }

        UpdateNextEventTimer(entity.Owner, entity.Comp1, currentTime);

        TryStartNewEvent(entity);
        ProcessActiveEvent(entity, currentTime);
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

    private void UpdateNextEventTimer(EntityUid uid, SupermatterEventComponent comp, TimeSpan currentTime)
    {
        if (comp.NextEventTimer <= TimeSpan.Zero)
            return;

        var elapsed = currentTime - comp.LastNextEventTimerUpdate;
        if (TryComp<SupermatterGasComponent>(uid, out var gasComp) && gasComp.RuntimeEventSpeedMultiplier > 1f)
            elapsed = TimeSpan.FromTicks((long) (elapsed.Ticks * gasComp.RuntimeEventSpeedMultiplier));

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

        if (comp.CurrentEvent == SupermatterEventComponent.SupermatterEventType.Radiation)
            _radiationSystem.SetIntensity(entity.Owner, comp.DefaultRadiationIntensity);

        var randomEvtIndex = _random.Next(0, comp.AllowedEventTypes.Count);
        var randomEvtType = comp.AllowedEventTypes[randomEvtIndex];

        if (!comp.SupermatterEventTypesToEvents.TryGetValue(randomEvtType, out var eventHandler))
            return;

        switch (eventHandler)
        {
            case SupermatterNoneEvent:
                SupermatterNoneEvent.Activate(entity, this);
                AnnounceFromSupermatterConsole(entity.Owner, SupermatterNoneEvent.GetAnnouncement());
                break;
            case SupermatterLightningEvent:
                SupermatterLightningEvent.Activate(entity, this);
                AnnounceFromSupermatterConsole(entity.Owner, SupermatterLightningEvent.GetAnnouncement());
                break;
            case SupermatterRadiationEvent:
                SupermatterRadiationEvent.Activate(entity, this);
                AnnounceFromSupermatterConsole(entity.Owner, SupermatterRadiationEvent.GetAnnouncement());
                break;
            case SupermatterPlasmaEvent:
                SupermatterPlasmaEvent.Activate(entity, this);
                AnnounceFromSupermatterConsole(entity.Owner, SupermatterPlasmaEvent.GetAnnouncement());
                break;
        }
    }

    private void ProcessActiveEvent(Entity<SupermatterEventComponent> entity, TimeSpan currentTime)
    {
        var comp = entity.Comp;

        if (comp.EventEndTime == TimeSpan.Zero)
            return;

        if (!comp.SupermatterEventTypesToEvents.TryGetValue(comp.CurrentEvent, out var eventHandler))
            return;

        switch (eventHandler)
        {
            case SupermatterNoneEvent:
                SupermatterNoneEvent.Process(entity, this, currentTime);
                break;
            case SupermatterLightningEvent:
                SupermatterLightningEvent.Process(entity, this, currentTime);
                break;
            case SupermatterRadiationEvent:
                SupermatterRadiationEvent.Process(entity, this, currentTime);
                break;
            case SupermatterPlasmaEvent:
                SupermatterPlasmaEvent.Process(entity, this, currentTime);
                break;
        }
    }

    private void AnnounceFromSupermatterConsole(EntityUid crystal, string message)
    {
        var ev = new SupermatterSendRadioEvent(message);
        RaiseLocalEvent(crystal, ref ev);
    }

    public void SetRadiation(EntityUid uid, float intensity)
    {
        EnsureComp<RadiationSourceComponent>(uid);
        _radiationSystem.SetIntensity(uid, intensity);
    }

    public bool TryGetComponent<T>(EntityUid uid, out T? component) where T : IComponent
    {
        return TryComp(uid, out component);
    }
}
