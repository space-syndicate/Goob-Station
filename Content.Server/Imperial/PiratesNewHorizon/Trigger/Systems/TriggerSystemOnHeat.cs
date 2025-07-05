/// <summary>
/// Triggers explosion on interacting with heat-items
/// Or just heated gases
/// </summary>

using Content.Server.Explosion.Components;
using Content.Shared.Examine;
using Content.Shared.Explosion.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Server.Explosion.Components;
using Content.Shared.Interaction;
using Content.Shared.Temperature;
using Content.Shared.Weapons.Melee.Events;
using Content.Server.Atmos.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Random;
using Content.Server.Imperial.PiratesNewHorizon.Trigger.Components;
using Content.Server.Explosion.EntitySystems;

namespace Content.Server.Imperial.PiratesNewHorizon.Trigger.Systems;

public sealed class TriggerSystemOnHeat : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly TriggerSystem _triggerSystem = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<TriggerOnHeatComponent, AttackedEvent>(OnAttacked);
        SubscribeLocalEvent<TriggerOnHeatComponent, InteractUsingEvent>(OnUsing);
    }
    public void OnRandomTimerHeatTriggerMapInit(Entity<RandomTimerTriggerComponent> ent, ref MapInitEvent args)
    {
        var (_, comp) = ent;

        if (!TryComp(ent, out TriggerOnHeatComponent? heatTriggerComp))
            return;

        heatTriggerComp.Delay = _random.NextFloat(comp.Min, comp.Max);
    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        List<Entity<TriggerOnHeatComponent>> toUpdate = new();
        var query = EntityQueryEnumerator<TriggerOnHeatComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var trigger, out var transform))
        {
            var environment = _atmosphereSystem.GetTileMixture((uid, transform));
            if (environment == null)
                continue;

            if (environment.Temperature < trigger.ActivationTemperature)
                continue;

            if(!TryComp(uid, out TriggerOnHeatComponent? heatTrigger))
                return;
            _triggerSystem.HandleTimerTrigger(
                uid,
                null,
                heatTrigger.Delay,
                heatTrigger.BeepInterval,
                heatTrigger.InitialBeepDelay,
                heatTrigger.BeepSound);
        }

        foreach (var a in toUpdate)
        {
            if(!TryComp(a, out TriggerOnHeatComponent? heatTrigger))
                return;
            _triggerSystem.HandleTimerTrigger(
                a,
                null,
                heatTrigger.Delay,
                heatTrigger.BeepInterval,
                heatTrigger.InitialBeepDelay,
                heatTrigger.BeepSound);
        }

    }

    private void OnAttacked(EntityUid uid, TriggerOnHeatComponent component, AttackedEvent args)
    {
        if (!component.ActivateHotItems || !CheckHot(args.Used))
            return;
        _triggerSystem.HandleTimerTrigger(
            uid,
            args.User,
            component.Delay,
            component.BeepInterval,
            component.InitialBeepDelay,
            component.BeepSound);
    }
    private void OnUsing(EntityUid uid, TriggerOnHeatComponent component, InteractUsingEvent args)
    {

        if (!component.ActivateHotItems || !CheckHot(args.Used))
            return;
        _triggerSystem.HandleTimerTrigger(
            uid,
            args.User,
            component.Delay,
            component.BeepInterval,
            component.InitialBeepDelay,
            component.BeepSound);
    }

    private bool CheckHot(EntityUid usedUid)
    {
        var hotEvent = new IsHotEvent();
        RaiseLocalEvent(usedUid, hotEvent);
        return hotEvent.IsHot;
    }
}