using Content.Server.Imperial.Power.Components;
using Content.Server.NukeOps;
using Content.Shared.Imperial.Power.Components;
using Content.Shared.Imperial.Power.Events;
using Content.Shared.NukeOps;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Imperial.Power.EntitySystems;

public sealed class SupermatterEventSchedulerSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _componentFactory = null!;
    [Dependency] private readonly IGameTiming _gameTiming = null!;
    [Dependency] private readonly IRobustRandom _random = null!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SupermatterEventSchedulerComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<WarDeclaredEvent>(OnWarOps);
        SubscribeLocalEvent<SupermatterEventSchedulerComponent, SupermatterEventFinishedEvent>(OnEventFinished);
        SubscribeLocalEvent<SupermatterIntegrityComponent, SupermatterTouchedEvent>(OnTouched);
    }

    private void OnInit(Entity<SupermatterEventSchedulerComponent> ent, ref ComponentInit args)
    {
        ent.Comp.FirstEventInterval ??= TimeSpan.FromSeconds(_random.Next(600, 900));
        ent.Comp.NextEventTime ??= _gameTiming.CurTime + ent.Comp.FirstEventInterval.Value;
    }

    private void OnWarOps(ref WarDeclaredEvent args)
    {
        if (args.Status != WarConditionStatus.YesWar)
            return;

        var query = EntityQueryEnumerator<SupermatterEventSchedulerComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            comp.Disabled = true;
            Log.Info($"Суперматерия ({uid}) более не может создавать события: война.");
        }
    }

    private void OnEventFinished(Entity<SupermatterEventSchedulerComponent> ent, ref SupermatterEventFinishedEvent args)
    {
        var multiplier = 1f;

        if (TryComp<SupermatterGasComponent>(ent, out var gasComp))
            multiplier = MathF.Max(gasComp.RuntimeEventSpeedMultiplier, 0.01f);

        ent.Comp.NextEventTime = _gameTiming.CurTime + args.NextEventInterval / multiplier;
        ent.Comp.ConcurrentEvents--;
    }

    private void OnTouched(Entity<SupermatterIntegrityComponent> ent, ref SupermatterTouchedEvent args)
    {
        if (TryComp<SupermatterGasComponent>(ent, out var gas) && gas.RuntimeDisableTouchGib)
        {
            args.Cancelled = true;
            return;
        }

        ent.Comp.Activated = true;
        if (TryComp<SupermatterEventSchedulerComponent>(ent, out var schedulerComp))
            schedulerComp.NextEventTime = _gameTiming.CurTime;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var currentTime = _gameTiming.CurTime;

        var query = EntityQueryEnumerator<SupermatterEventSchedulerComponent, SupermatterIntegrityComponent>();

        while (query.MoveNext(out var uid, out var schedComp, out var integrityComp))
        {
            if (!integrityComp.Activated || schedComp.Disabled)
                continue;

            if (currentTime > schedComp.NextEventTime)
                StartRandomEvent((uid, schedComp));
        }
    }

    private void StartRandomEvent(Entity<SupermatterEventSchedulerComponent> ent)
    {
        if (ent.Comp.Events is { Components.Length: 0 } || ent.Comp.ConcurrentEvents >= ent.Comp.MaxConcurrentEvents)
            return;

        var availableEvents = new List<ComponentRegistration>();

        if (ent.Comp.Events is { Components: not null })
        {
            foreach (var eventName in ent.Comp.Events.Components)
            {
                if (!_componentFactory.TryGetRegistration(eventName, out var registration))
                {
                    Log.Error($"Не найден компонент события: {eventName}");
                    continue;
                }

                if (!HasComp(ent, registration.Type))
                    availableEvents.Add(registration);
            }
        }

        if (availableEvents.Count == 0)
            return;
        var randomEvent = _random.Pick(availableEvents);

        ent.Comp.NextEventTime = TimeSpan.MaxValue;
        ent.Comp.ConcurrentEvents++;
        var newComponent = _componentFactory.GetComponent(randomEvent);
        AddComp(ent, newComponent);
    }
}
