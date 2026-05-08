// SPDX-FileCopyrightText: 2023 Kara <lunarautomaton6@gmail.com>
// SPDX-FileCopyrightText: 2024 Nemanja <98561806+EmoGarbage404@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Plykiya <58439124+Plykiya@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 plykiya <plykiya@protonmail.com>
// SPDX-FileCopyrightText: 2024 themias <89101928+themias@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.ActionBlocker;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Input;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;

namespace Content.Shared.Interaction;

/// <summary>
/// This handles smart equipping or inserting/ejecting from slots through keybinds--generally shift+E and shift+B
/// </summary>
public sealed class SmartEquipSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly ItemSlotsSystem _slots = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        CommandBinds.Builder
            .Bind(ContentKeyFunctions.SmartEquipBackpack, InputCmdHandler.FromDelegate(HandleSmartEquipBackpack, handle: false, outsidePrediction: false))
            .Bind(ContentKeyFunctions.SmartEquipBelt, InputCmdHandler.FromDelegate(HandleSmartEquipBelt, handle: false, outsidePrediction: false))
            .Register<SmartEquipSystem>();
    }

    public override void Shutdown()
    {
        base.Shutdown();

        CommandBinds.Unregister<SmartEquipSystem>();
    }

    private void HandleSmartEquipBackpack(ICommonSession? session)
    {
        HandleSmartEquip(session, "back");
    }

    private void HandleSmartEquipBelt(ICommonSession? session)
    {
        HandleSmartEquip(session, "belt");
    }

    private void HandleSmartEquip(ICommonSession? session, string equipmentSlot)
    {
        if (session is not { } playerSession)
            return;

        if (playerSession.AttachedEntity is not { Valid: true } uid || !Exists(uid))
            return;

        // early out if we don't have any hands or a valid inventory slot
        if (!TryComp<HandsComponent>(uid, out var hands) || hands.ActiveHandId == null)
            return;

        var handItem = _hands.GetActiveItem((uid, hands));

        // can the user interact, and is the item interactable? e.g. virtual items
        if (!_actionBlocker.CanInteract(uid, handItem))
            return;

        if (!TryComp<InventoryComponent>(uid, out var inventory) || !_inventory.HasSlot(uid, equipmentSlot, inventory))
        {
            _popup.PopupClient(Loc.GetString("smart-equip-missing-equipment-slot", ("slotName", equipmentSlot)), uid, uid);
            return;
        }

        // early out if we have an item and cant drop it at all
        if (handItem != null && !_hands.CanDropHeld(uid, hands.ActiveHandId))
        {
            _popup.PopupClient(Loc.GetString("smart-equip-cant-drop"), uid, uid);
            return;
        }

        // There are eight main cases we want to handle here,
        // so let's write them out

        // if the slot we're trying to smart equip from:
        // 1) doesn't have an item
        //    - with hand item: try to put it in the slot
        //    - without hand item: fail
        // CorvaxGoob edit start
        // 2) has an item, and that item is an item slots holder
        //    - with hand item: get the highest priority item slot with a valid whitelist and try to insert it;
        //                      if no valid item slot found, try storage as fallback
        //    - without hand item: get the highest priority item slot with an item and try to eject it;
        //                         if no item slot has an item, try storage as fallback
        // 3) has an item, and that item is a storage item
        //    - with hand item: try to put it in storage
        //    - without hand item: try to take the last stored item and put it in our hands
        // CorvaxGoob edit end
        // 4) has an item, with no special storage components
        //    - with hand item: fail
        //    - without hand item: try to put the item into your hand

        _inventory.TryGetSlotEntity(uid, equipmentSlot, out var slotEntity);
        var emptyEquipmentSlotString = Loc.GetString("smart-equip-empty-equipment-slot", ("slotName", equipmentSlot));

        // case 1 (no slot item):
        if (slotEntity is not { } slotItem)
        {
            if (handItem == null)
            {
                _popup.PopupClient(emptyEquipmentSlotString, uid, uid);
                return;
            }

            if (!_inventory.CanEquip(uid, handItem.Value, equipmentSlot, out var reason))
            {
                _popup.PopupClient(Loc.GetString(reason), uid, uid);
                return;
            }

            _hands.TryDrop((uid, hands), hands.ActiveHandId!);
            _inventory.TryEquip(uid, handItem.Value, equipmentSlot, predicted: true, checkDoafter: true);
            return;
        }

        // CorvaxGoob edit start

        // case 2 (itemslot item):
        if (TryComp<ItemSlotsComponent>(slotItem, out var slots))
        {
            if (handItem == null)
            {
                ItemSlot? toEjectFrom = null;

                foreach (var slot in slots.Slots.Values)  // CorvaxGoob edit
                {
                    if (slot.HasItem
                    && _slots.CanEject(slotItem, uid, slot)
                    && slot.Priority > (toEjectFrom?.Priority ?? int.MinValue))
                    toEjectFrom = slot;
                }

                // CorvaxGoob edit start
                if (toEjectFrom == null)
                {
                    // No item in any ItemSlot - try Storage as an explicit fallback.
                    // Needed for items that have both components.
                    if (TryComp<StorageComponent>(slotItem, out var ejectStorage)
                        && ejectStorage.Container.ContainedEntities.Count > 0)
                    {
                        var removing = ejectStorage.Container.ContainedEntities[^1];
                        _container.RemoveEntity(slotItem, removing);
                        _hands.TryPickup(uid, removing, handsComp: hands);
                        return;
                    }

                    _popup.PopupClient(emptyEquipmentSlotString, uid, uid);
                    return;
                }
                // CorvaxGoob edit end

                _slots.TryEjectToHands(slotItem, toEjectFrom, uid, excludeUserAudio: true);
                return;
            }

            ItemSlot? toInsertTo = null;

            foreach (var slot in slots.Slots.Values)
            {
                if (!slot.HasItem
                    && _whitelistSystem.IsWhitelistPassOrNull(slot.Whitelist, handItem.Value)
                    && slot.Priority > (toInsertTo?.Priority ?? int.MinValue))
                {
                    toInsertTo = slot;
                }
            }

            // CorvaxGoob edit start
            if (toInsertTo != null)
            {
                _slots.TryInsertFromHand(slotItem, toInsertTo, uid, hands, excludeUserAudio: true);
                return;
            }

            // No valid ItemSlot found - try Storage as an explicit fallback.
            // Needed for items that have both components.
            if (TryComp<StorageComponent>(slotItem, out var fallbackStorage))
            {
                TryInsertIntoStorage(slotItem, handItem.Value, uid, hands.ActiveHandId!, hands, fallbackStorage);
                return;
            }

            _popup.PopupClient(Loc.GetString("smart-equip-no-valid-item-slot-insert", ("item", handItem.Value)), uid, uid);
            return;
            // CorvaxGoob edit end
        }

        // case 3 (storage item):
        if (TryComp<StorageComponent>(slotItem, out var storage))
        {
            switch (handItem)
            {
                case null when storage.Container.ContainedEntities.Count == 0:
                    _popup.PopupClient(emptyEquipmentSlotString, uid, uid);
                    return;
                case null:
                    var removing = storage.Container.ContainedEntities[^1];
                    _container.RemoveEntity(slotItem, removing);
                    _hands.TryPickup(uid, removing, handsComp: hands);
                    return;
            }

            TryInsertIntoStorage(slotItem, handItem.Value, uid, hands.ActiveHandId!, hands, storage); // CorvaxGoob edit
            return;
        }

        // CorvaxGoob edit end

        // case 4 (just an item):
        if (handItem != null)
            return;

        if (!_inventory.CanUnequip(uid, equipmentSlot, out var inventoryReason))
        {
            _popup.PopupClient(Loc.GetString(inventoryReason), uid, uid);
            return;
        }

        _inventory.TryUnequip(uid, equipmentSlot, inventory: inventory, predicted: true, checkDoafter: true);
        _hands.TryPickup(uid, slotItem, handsComp: hands);
    }

    // CorvaxGoob edit start
    private void TryInsertIntoStorage(
        EntityUid slotItem,
        EntityUid handItem,
        EntityUid uid,
        string activeHandId,
        HandsComponent hands,
        StorageComponent storage)
    {
        if (!_storage.CanInsert(slotItem, handItem, out var reason))
        {
            if (reason != null)
                _popup.PopupClient(Loc.GetString(reason), uid, uid);
            return;
        }

        _hands.TryDrop((uid, hands), activeHandId);
        _storage.Insert(slotItem, handItem, out var stacked, out _);

        // if the hand item stacked with the things in inventory but theres no more space left for the rest
        // of the stack place the stack back in hand rather not drop it on the floor
        if (stacked != null && !_storage.CanInsert(slotItem, handItem, out _))
        {
            if (TryComp<StackComponent>(handItem, out var handStack) && handStack.Count > 0)
                _hands.TryPickup(uid, handItem, handsComp: hands);
        }
    }
    // CorvaxGoob edit end
}
