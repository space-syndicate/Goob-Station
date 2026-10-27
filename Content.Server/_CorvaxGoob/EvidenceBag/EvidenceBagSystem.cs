using Content.Server.Destructible;
using Content.Server.Item;
using Content.Shared._CorvaxGoob.EvidenceBag;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Item;
using Content.Shared.Lock;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;

namespace Content.Server._CorvaxGoob.EvidenceBag;

public sealed partial class EvidenceBagSystem : EntitySystem
{
    [Dependency] private readonly DestructibleSystem _destructible = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly ItemSlotsSystem _slots = default!;
    [Dependency] private readonly ItemSystem _item = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EvidenceBagComponent, LockToggledEvent>(OnLockToggled);
        SubscribeLocalEvent<EvidenceBagComponent, EntInsertedIntoContainerMessage>(OnEntInsertedIntoContainer);
    }

    private void OnEntInsertedIntoContainer(Entity<EvidenceBagComponent> entity, ref EntInsertedIntoContainerMessage ev)
    {
        if (ev.Container.ID != entity.Comp.Slot)
            return;

        if (!TryComp<ItemComponent>(ev.Entity, out var itemComp))
            return;

        _meta.SetEntityName(entity, Loc.GetString("evidence-bag-name-with", ("name", MetaData(ev.Entity).EntityName)));

        _item.SetSize(entity, itemComp.Size);
        _item.SetShape(entity, itemComp.Shape);
    }

    private void OnLockToggled(Entity<EvidenceBagComponent> entity, ref LockToggledEvent ev)
    {
        if (ev.Locked)
        {
            if (_slots.GetItemOrNull(entity, "slot") is not { } item)
                return;

            if (!TryComp<ItemComponent>(item, out var itemComp))
                return;

            _meta.SetEntityName(entity, Loc.GetString("evidence-bag-name-with", ("name", MetaData(item).EntityName)));

            _item.SetSize(entity, itemComp.Size);
        }
        else
            _destructible.DestroyEntity((entity, MetaData(entity)));
    }
}
