using Content.Client._White.ItemSlotRenderer;
using Content.Shared._CorvaxGoob.EvidenceBag;
using Content.Shared.Item;
using Robust.Shared.Containers;

namespace Content.Client._CorvaxGoob.EvidenceBag;

public sealed partial class EvidenceBagSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EvidenceBagComponent, EntInsertedIntoContainerMessage>(OnEntInsertedIntoContainer);
    }

    private void OnEntInsertedIntoContainer(Entity<EvidenceBagComponent> entity, ref EntInsertedIntoContainerMessage ev)
    {
        if (ev.Container.ID != entity.Comp.Slot)
            return;

        if (!TryComp<ItemComponent>(ev.Entity, out var itemComp))
            return;

        if (!TryComp<ItemSlotRendererComponent>(ev.Container.Owner, out var itemSlot))
            return;

        if (!entity.Comp.SizesInfo.TryGetValue(itemComp.Size, out var size))
            return;

        itemSlot.RenderSlotScale = size;
    }
}
