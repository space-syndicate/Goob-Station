using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Trigger;
using Content.Shared._CorvaxGoob.Trigger.Components.Triggers;

namespace Content.Shared._CorvaxGoob.Trigger.Systems;

public sealed class StaminaDrainOnTriggerSystem : XOnTriggerSystem<StaminaDrainOnTriggerComponent>
{
    private const string DrainKey = "StaminaDrainOnTrigger";

    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;

    protected override void OnTrigger(Entity<StaminaDrainOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        foreach (var uid in _lookup.GetEntitiesInRange(ent.Owner, ent.Comp.Radius))
        {
            _stamina.ToggleStaminaDrain(uid, ent.Comp.StaminaDamagePerSecond, true, false, DrainKey, source: ent.Owner, applyResistances: false);
        }

        args.Handled = true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<StaminaComponent, ActiveStaminaComponent>();
        while (query.MoveNext(out var uid, out var stamina, out _))
        {
            if (stamina.Critical && stamina.ActiveDrains.ContainsKey(DrainKey))
                _stamina.ToggleStaminaDrain(uid, 0f, false, false, DrainKey);
        }
    }
}
