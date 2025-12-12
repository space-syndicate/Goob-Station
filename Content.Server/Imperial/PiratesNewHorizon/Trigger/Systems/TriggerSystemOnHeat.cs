/// <summary>
/// Triggers explosion on interacting with heat-items
/// Or just heated gases
/// </summary>

using Content.Shared.Interaction;
using Content.Shared.Temperature;
using Content.Shared.Weapons.Melee.Events;
using Content.Server.Atmos.EntitySystems;
using Robust.Shared.Random;
using Content.Server.Imperial.PiratesNewHorizon.Trigger.Components;
using Content.Server.Explosion.EntitySystems;
using Content.Shared.Trigger.Components;
using Content.Shared.Trigger.Systems;

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

        SubscribeLocalEvent<TriggerOnHeatComponent, MapInitEvent>(OnRandomTimerHeatTriggerMapInit, after: new[] { typeof(TriggerSystem) });
    }
    public void OnRandomTimerHeatTriggerMapInit(Entity<TriggerOnHeatComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<RandomTimerTriggerComponent>(ent, out var randomTimerTriggerComponent))
            return;

        ent.Comp.Delay = _random.NextFloat(randomTimerTriggerComponent.Min, randomTimerTriggerComponent.Max);
    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<TriggerOnHeatComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var trigger, out var transform))
        {
            var environment = _atmosphereSystem.GetTileMixture((uid, transform));
            if (environment == null)
                continue;

            if (environment.Temperature < trigger.ActivationTemperature)
                continue;

            if (!TryComp(uid, out TriggerOnHeatComponent? heatTrigger))
                return;

            _triggerSystem.ActivateTimerTrigger(uid);
        }
    }

    private void OnAttacked(EntityUid uid, TriggerOnHeatComponent component, AttackedEvent args)
    {
        if (!component.ActivateHotItems || !CheckHot(args.Used))
            return;
        _triggerSystem.ActivateTimerTrigger(
            uid,
            args.User);
    }
    private void OnUsing(EntityUid uid, TriggerOnHeatComponent component, InteractUsingEvent args)
    {
        if (!component.ActivateHotItems || !CheckHot(args.Used))
            return;

        _triggerSystem.ActivateTimerTrigger(
            uid,
            args.User
        );
    }

    private bool CheckHot(EntityUid usedUid)
    {
        var hotEvent = new IsHotEvent();
        RaiseLocalEvent(usedUid, hotEvent);
        return hotEvent.IsHot;
    }
}
