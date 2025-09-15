using Content.Server._CorvaxGoob.Events.Components;
using Content.Server.Explosion.EntitySystems;

namespace Content.Server._CorvaxGoob.Events.EntitySystems;

public sealed class ExecuteTargetEventOnTriggerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ExecuteTargetEventOnTriggerComponent, TriggerEvent>(OnTrigger);
    }

    private void OnTrigger(Entity<ExecuteTargetEventOnTriggerComponent> entity, ref TriggerEvent ev)
    {
        foreach (var targetEvent in entity.Comp.Events)
            RaiseLocalEvent(entity, (object) targetEvent, true);
    }
}
