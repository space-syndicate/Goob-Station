using Content.Shared.Trigger;
using Content.Shared.Storage.Components;
using Content.Shared._CorvaxGoob.Trigger.Components.Triggers;

namespace Content.Shared._CorvaxGoob.Trigger.Systems;

public sealed class TriggerOnOpenSystem : TriggerOnXSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TriggerOnOpenComponent, StorageAfterOpenEvent>(OnStorageOpen);
    }

    private void OnStorageOpen(Entity<TriggerOnOpenComponent> ent, ref StorageAfterOpenEvent args)
    {
        Trigger.Trigger(ent.Owner, null, ent.Comp.KeyOut);

        if (ent.Comp.RemoveOnTrigger)
          RemCompDeferred<TriggerOnOpenComponent>(ent.Owner);
    }
}