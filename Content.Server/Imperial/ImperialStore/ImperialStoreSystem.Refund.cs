using Content.Shared.Imperial.ImperialStore;
using Robust.Shared.Containers;

namespace Content.Server.Imperial.ImperialStore;

public sealed partial class ImperialStoreSystem
{
    private void InitializeRefund()
    {
        SubscribeLocalEvent<ImperialStoreComponent, EntityTerminatingEvent>(OnStoreTerminating);
        SubscribeLocalEvent<ImperialStoreRefundComponent, EntityTerminatingEvent>(OnRefundTerminating);
        SubscribeLocalEvent<ImperialStoreRefundComponent, EntRemovedFromContainerMessage>(OnEntityRemoved);
        SubscribeLocalEvent<ImperialStoreRefundComponent, EntInsertedIntoContainerMessage>(OnEntityInserted);
    }

    private void OnEntityRemoved(EntityUid uid, ImperialStoreRefundComponent component, EntRemovedFromContainerMessage args)
    {
        if (component.StoreEntity == null || _actions.GetAction(uid) == null || !TryComp<ImperialStoreComponent>(component.StoreEntity.Value, out var storeComp))
            return;

        DisableRefund(component.StoreEntity.Value, storeComp);
    }

    private void OnEntityInserted(EntityUid uid, ImperialStoreRefundComponent component, EntInsertedIntoContainerMessage args)
    {
        if (component.StoreEntity == null || _actions.GetAction(uid) == null || !TryComp<ImperialStoreComponent>(component.StoreEntity.Value, out var storeComp))
            return;

        DisableRefund(component.StoreEntity.Value, storeComp);
    }

    private void OnStoreTerminating(Entity<ImperialStoreComponent> ent, ref EntityTerminatingEvent args)
    {
        if (ent.Comp.BoughtEntities.Count <= 0)
            return;

        foreach (var boughtEnt in ent.Comp.BoughtEntities)
        {
            if (!TryComp<ImperialStoreRefundComponent>(boughtEnt, out var refundComp))
                continue;

            refundComp.StoreEntity = null;
        }
    }

    private void OnRefundTerminating(Entity<ImperialStoreRefundComponent> ent, ref EntityTerminatingEvent args)
    {
        if (ent.Comp.StoreEntity == null)
            return;

        var ev = new ImperialRefundEntityDeletedEvent(ent);
        RaiseLocalEvent(ent.Comp.StoreEntity.Value, ref ev);
    }
}
