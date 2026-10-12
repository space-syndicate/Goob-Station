using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Shared._Aquila.PersonalLoadouts;

/// <summary>
/// Выдаёт личные вещи из лоадаута. Личная вещь занимает слот, а то, что в нём было,
/// убирается в рюкзак или в руки, а не падает на пол.
/// </summary>
public sealed class PersonalLoadoutSystem : EntitySystem
{
    /// <summary>
    /// Группа лоадаутов, в которой лежат личные вещи.
    /// </summary>
    public static readonly ProtoId<LoadoutGroupPrototype> Group = "Inventory";

    private const string BackSlot = "back";

    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    /// <summary>
    /// Выдаёт сущности все выбранные личные вещи из лоадаута роли.
    /// </summary>
    public void EquipPersonalLoadouts(EntityUid uid, RoleLoadout loadout)
    {
        if (!loadout.SelectedLoadouts.TryGetValue(Group, out var selected))
            return;

        foreach (var item in selected)
        {
            if (!_prototype.Resolve(item.Prototype, out var proto))
                continue;

            if (_prototype.Resolve(proto.StartingGear, out var startingGear))
                Equip(uid, startingGear);

            Equip(uid, proto);
        }
    }

    private void Equip(EntityUid uid, IEquipmentLoadout gear)
    {
        var coords = Transform(uid).Coordinates;

        foreach (var (slot, protoId) in gear.Equipment)
        {
            EquipToSlot(uid, slot, Spawn(protoId, coords));
        }

        foreach (var protoId in gear.Inhand)
        {
            var spawned = Spawn(protoId, coords);
            if (!_hands.TryPickupAnyHand(uid, spawned, checkActionBlocker: false, animate: false))
                Stash(uid, spawned);
        }

        foreach (var (slot, protoIds) in gear.Storage)
        {
            foreach (var protoId in protoIds)
            {
                var spawned = Spawn(protoId, coords);
                if (!TryInsertIntoSlotStorage(uid, slot, spawned))
                    Stash(uid, spawned);
            }
        }
    }

    private void EquipToSlot(EntityUid uid, string slot, EntityUid item)
    {
        if (!_inventory.TryGetSlot(uid, slot, out var slotDefinition)
            || slotDefinition.Whitelist != null && !_whitelist.IsWhitelistPass(slotDefinition.Whitelist, item))
        {
            Stash(uid, item);
            return;
        }

        if (!_inventory.TryGetSlotEntity(uid, slot, out _))
        {
            if (!_inventory.TryEquip(uid, item, slot, silent: true, force: true))
                Stash(uid, item);

            return;
        }

        if (HasOccupiedDependents(uid, slot)
            || !_inventory.TryUnequip(uid, slot, out var old, silent: true, force: true))
        {
            Stash(uid, item);
            return;
        }

        if (!_inventory.TryEquip(uid, item, slot, silent: true, force: true))
        {
            _inventory.TryEquip(uid, old.Value, slot, silent: true, force: true);
            Stash(uid, item);
            return;
        }

        Stash(uid, old.Value);
    }

    private bool HasOccupiedDependents(EntityUid uid, string slot)
    {
        if (!_inventory.TryGetSlots(uid, out var slots))
            return false;

        foreach (var definition in slots)
        {
            if (definition.DependsOn == slot && _inventory.TryGetSlotEntity(uid, definition.Name, out _))
                return true;
        }

        return false;
    }

    private bool TryInsertIntoSlotStorage(EntityUid uid, string slot, EntityUid item)
    {
        return _inventory.TryGetSlotEntity(uid, slot, out var slotEntity)
               && TryComp<StorageComponent>(slotEntity, out var storage)
               && _storage.Insert(slotEntity.Value, item, out _, storageComp: storage, playSound: false);
    }

    private void Stash(EntityUid uid, EntityUid item)
    {
        if (TryInsertIntoSlotStorage(uid, BackSlot, item))
            return;

        _hands.TryPickupAnyHand(uid, item, checkActionBlocker: false, animate: false);
    }
}
