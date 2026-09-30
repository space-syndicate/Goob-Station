// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using Content.Server.Power.EntitySystems;
using Content.Server.Wires;
using Content.Shared.Construction.EntitySystems;
using Content.Shared.Destructible;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.VendingMachines;
using Content.Shared.Wall;
using Content.Shared.Wires;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Server.VendingMachines
{
    public sealed partial class VendingMachineSystem
    {
        [Dependency] private SharedContainerSystem _container = default!;
        [Dependency] private SharedHandsSystem _hands = default!;

        /// <summary>
        /// Subscribes server handlers for returned-item vending.
        /// </summary>
        private void InitializeVendingReturn()
        {
            SubscribeLocalEvent<VendingMachineComponent, ComponentInit>(OnComponentInit);

            // Some closed Openable items handle fallback interactions before the vending machine can see them.
            // Use InteractUsingEvent so those items can still be returned before OpenableSystem blocks the click.
            SubscribeLocalEvent<VendingMachineComponent, InteractUsingEvent>(
                OnInteractUsing,
                after: [typeof(WiresSystem), typeof(AnchorableSystem)]);

            SubscribeLocalEvent<VendingMachineComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<VendingMachineComponent, DestructionEventArgs>(OnDestruction);
            SubscribeLocalEvent<VendingMachineComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAlternativeVerbs);
        }

        /// <summary>
        /// Creates the hidden container used to store returned item entities.
        /// </summary>
        private void OnComponentInit(Entity<VendingMachineComponent> ent, ref ComponentInit args)
        {
            // Returned items are real entities stored in the machine, while vending inventory only tracks prototype counts.
            ent.Comp.ReturnedInventoryContainer = _container.EnsureContainer<Container>(ent, VendingMachineComponent.ReturnedInventoryContainerId);
        }

        /// <summary>
        /// Handles early item interactions so returnable closed items are not blocked by fallback systems.
        /// </summary>
        private void OnInteractUsing(EntityUid uid, VendingMachineComponent component, InteractUsingEvent args)
        {
            if (args.Handled)
                return;

            // Only consume the click when the item was actually accepted, preserving normal interactions otherwise.
            args.Handled = TryReturnItem((uid, component), args.User, args.Used);
        }

        /// <summary>
        /// Attempts to move a returnable item into the vending machine.
        /// </summary>
        /// <returns>True if the item was accepted.</returns>
        private bool TryReturnItem(Entity<VendingMachineComponent> vending, EntityUid user, EntityUid used)
        {
            var (uid, component) = vending;

            // The item to vend is chosen after the eject delay, so do not let returns change that pool mid-vend.
            if (component.Ejecting)
                return false;

            // Restock boxes have their own interaction path and must not be handled by the new return flow.
            if (HasComp<VendingMachineRestockComponent>(used))
                return false;

            // Only accept item types that already exist in this machine's configured inventory.
            var prototype = MetaData(used).EntityPrototype?.ID;
            if (prototype == null || !TryGetReturnableEntry(component, prototype, out var entry))
                return false;

            // Do not accept returned items if the machine is broken or has no power.
            if (component.Broken || !this.IsPowered(uid, EntityManager))
                return false;

            // Try to move the item into hidden storage before counting it as returned.
            if (!_hands.TryDropIntoContainer(user, used, component.ReturnedInventoryContainer))
                return false;

            // Store the returned entity by prototype so the machine can vend that exact item instead of a new entity.
            component.ReturnedInventory ??= new();
            component.ReturnedInventory.GetOrNew(prototype).Add(used);
            entry.Amount++;
            Dirty(uid, component);
            UpdateUI((uid, component));

            Popup.PopupEntity(Loc.GetString("vending-machine-component-return-success",
                ("item", used),
                ("target", uid)), user, user);
            return true;
        }

        /// <summary>
        /// Finds an inventory entry that can accept the returned prototype.
        /// </summary>
        /// <returns>True if the prototype exists in regular, emagged, or contraband stock.</returns>
        private static bool TryGetReturnableEntry(
            VendingMachineComponent component,
            string prototype,
            [NotNullWhen(true)] out VendingMachineInventoryEntry? entry)
        {
            // Returned items can refill regular, emagged, or contraband inventory entries.
            if (component.Inventory.TryGetValue(prototype, out entry) ||
                component.EmaggedInventory.TryGetValue(prototype, out entry) ||
                component.ContrabandInventory.TryGetValue(prototype, out entry))
                return true;

            entry = null;
            return false;
        }

        /// <summary>
        /// Shows an examine hint when real returned items are stored inside the machine.
        /// </summary>
        private void OnExamined(EntityUid uid, VendingMachineComponent component, ExaminedEvent args)
        {
            if (!HasStoredReturnedItems(component))
                return;

            var message = Loc.GetString("vending-machine-component-returned-items-examine");
            args.PushMarkup($"[color=yellow]{message}[/color]");
        }

        /// <summary>
        /// Checks whether the machine still contains at least one valid returned item.
        /// </summary>
        private bool HasStoredReturnedItems(VendingMachineComponent component)
        {
            if (component.ReturnedInventory is null)
                return false;

            foreach (var returned in component.ReturnedInventory.Values)
            {
                foreach (var item in returned)
                {
                    // Only show the examine hint for items still physically stored inside this machine.
                    if (!Deleted(item) && component.ReturnedInventoryContainer.Contains(item))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Counts valid returned items of one prototype without mutating vending state.
        /// </summary>
        private int GetStoredReturnedItemCount(VendingMachineComponent component, string itemId)
        {
            if (component.ReturnedInventory is null ||
                !component.ReturnedInventory.TryGetValue(itemId, out var returned))
                return 0;

            var count = 0;

            foreach (var item in returned)
            {
                // Price calculation is read-only, so stale entries are ignored instead of being cleaned here.
                if (!Deleted(item) && component.ReturnedInventoryContainer.Contains(item))
                    count++;
            }

            return count;
        }

        /// <summary>
        /// /// Removes stale returned-item entries for the requested prototype before vending checks availability.
        /// </summary>
        /// <returns>True if returned tracking or stock changed.</returns>
        protected override bool CleanupReturnedInventoryBeforeVend(EntityUid uid, VendingMachineComponent component, string itemId)
        {
            return CleanupStaleReturnedItems(component, itemId, updateInventory: true);
        }

        /// <summary>
        /// Removes stale returned-item entries from all tracked prototypes.
        /// </summary>
        /// <returns>True if returned tracking or stock changed.</returns>
        private bool CleanupStaleReturnedInventory(VendingMachineComponent component, bool updateInventory)
        {
            if (component.ReturnedInventory is null)
                return false;

            if (component.ReturnedInventory.Count == 0)
            {
                component.ReturnedInventory = null;
                return true;
            }

            var changed = false;

            // Cleanup can remove entries from the dictionary, so iterate over a copy of the keys.
            foreach (var itemId in new List<string>(component.ReturnedInventory.Keys))
            {
                changed |= CleanupStaleReturnedItems(component, itemId, updateInventory);
            }

            return changed;
        }

        /// <summary>
        /// Removes invalid returned-item references for one prototype entry.
        /// </summary>
        /// <returns>True if returned tracking or stock changed.</returns>
        private bool CleanupStaleReturnedItems(VendingMachineComponent component, string itemId, bool updateInventory)
        {
            if (component.ReturnedInventory is null ||
                !component.ReturnedInventory.TryGetValue(itemId, out var returned))
                return false;

            var changed = false;
            VendingMachineInventoryEntry? entry = null;

            if (updateInventory)
                TryGetReturnableEntry(component, itemId, out entry);

            for (var index = returned.Count - 1; index >= 0; index--)
            {
                var item = returned[index];

                if (!Deleted(item) && component.ReturnedInventoryContainer.Contains(item))
                    continue;

                returned.RemoveAt(index);
                changed = true;

                // Each removed reference was counted in Amount, so decrement the entry during stock sync.
                if (entry != null && entry.Amount > 0)
                    entry.Amount--;
            }

            if (returned.Count == 0)
            {
                component.ReturnedInventory.Remove(itemId);
                changed = true;
            }

            if (component.ReturnedInventory.Count == 0)
                component.ReturnedInventory = null;

            return changed;
        }

        /// <summary>
        /// Drops stored returned items when the vending machine is destroyed.
        /// </summary>
        private void OnDestruction(EntityUid uid, VendingMachineComponent component, DestructionEventArgs args)
        {
            // When the vending machine is destroyed, all previously returned items stored inside it fall out.
            TryEjectStoredReturnedItems(uid, component, updateInventory: false, out _);
        }

        /// <summary>
        /// Adds the maintenance verb for removing stored returned items.
        /// </summary>
        private void OnGetAlternativeVerbs(EntityUid uid, VendingMachineComponent component, GetVerbsEvent<AlternativeVerb> args)
        {
            // Show the verb as long as there is something real to remove.
            // Panel, power, and access checks happen in Act so the user gets a specific failure popup.
            if (!args.CanInteract || component.Ejecting || !HasStoredReturnedItems(component))
                return;

            var verb = new AlternativeVerb
            {
                Text = Loc.GetString("vending-machine-component-remove-returned-items"),
                Act = () => RemoveReturnedItems(uid, args.User),
                Priority = 2,
            };

            args.Verbs.Add(verb);
        }

        /// <summary>
        /// Removes all stored returned items through the maintenance verb.
        /// </summary>
        private void RemoveReturnedItems(EntityUid uid, EntityUid user)
        {
            if (!TryComp<VendingMachineComponent>(uid, out var component))
                return;

            // The menu hides empty machines, but returned items can be removed or deleted before the click arrives.
            if (component.Ejecting || !HasStoredReturnedItems(component))
                return;

            // A powerless machine cannot operate its internal storage, but should not play an electronic deny sound.
            if (!this.IsPowered(uid, EntityManager))
            {
                Popup.PopupEntity(Loc.GetString("vending-machine-component-remove-returned-items-no-power"), uid, user, PopupType.MediumCaution);
                return;
            }

            // Removing stored items is maintenance work, so require the service panel to be opened first.
            if (!TryComp<WiresPanelComponent>(uid, out var panel) || !panel.Open)
            {
                Popup.PopupEntity(Loc.GetString("vending-machine-component-remove-returned-items-panel-closed"), uid, user, PopupType.MediumCaution);
                Deny((uid, component), user);

                // Deny() uses predicted audio and excludes the clicking user, so play the deny sound for them directly.
                Audio.PlayEntity(component.SoundDeny, user, uid, AudioParams.Default.WithVolume(-2f));
                return;
            }

            // Use normal vending-machine deny feedback for unauthorized users.
            if (!IsAuthorized(uid, user, component))
            {
                Popup.PopupEntity(Loc.GetString("vending-machine-component-try-eject-access-denied"), uid, user, PopupType.MediumCaution);

                // Keep the manual removal feedback consistent with normal unauthorized vending attempts.
                Audio.PlayEntity(component.SoundDeny, user, uid, AudioParams.Default.WithVolume(-2f));
                return;
            }

            var ejectedAny = TryEjectStoredReturnedItems(uid, component, updateInventory: true, out var changed);
            if (!ejectedAny)
            {
                // Cleanup may change tracking without ejecting an entity.
                if (changed)
                {
                    Dirty(uid, component);
                    UpdateUI((uid, component));
                }

                return;
            }

            Dirty(uid, component);
            UpdateUI((uid, component));

            // This verb runs server-side, so broadcast the vend sound instead of relying on prediction.
            Audio.PlayPvs(component.SoundVend, uid);
        }

        /// <summary>
        /// Moves all stored returned items out of the vending machine.
        /// </summary>
        /// <returns>True if at least one item was ejected.</returns>
        private bool TryEjectStoredReturnedItems(EntityUid uid, VendingMachineComponent component, bool updateInventory, out bool changed)
        {
            changed = false;

            if (component.ReturnedInventory is null)
                return false;

            var ejectedAny = false;
            var coordinates = GetReturnedItemEjectCoordinates(uid);
            var returnedInventory = component.ReturnedInventory;

            // Removal can delete entries from the dictionary, so iterate over a copy of the keys.
            foreach (var itemId in new List<string>(returnedInventory.Keys))
            {
                while (true)
                {
                    var removedItem = TryRemoveStoredReturnedItem(component, itemId, coordinates, updateInventory, out _, out var itemChanged);
                    changed |= itemChanged;

                    if (!removedItem)
                        break;

                    ejectedAny = true;

                    // Manual removal must undo the stock increase that happened when the item was returned.
                    // Destruction skips this because the vending machine and its inventory are being deleted anyway.
                    if (updateInventory &&
                        TryGetReturnableEntry(component, itemId, out var entry) &&
                        entry.Amount > 0)
                        entry.Amount--;
                }
            }

            if (returnedInventory.Count == 0)
            {
                component.ReturnedInventory = null;
                changed = true;
            }

            return ejectedAny;
        }

        /// <summary>
        /// Gets where returned items should appear when removed from storage.
        /// </summary>
        private EntityCoordinates GetReturnedItemEjectCoordinates(EntityUid uid)
        {
            var xform = Transform(uid);
            var coordinates = xform.Coordinates;

            // Wall-mounted vendors are anchored to a wall tile, so returned items should drop in front of the machine.
            // Use the same offset as normal vending so manual removal and destruction do not place items behind the wall.
            if (TryComp<WallMountComponent>(uid, out var wallMountComponent))
            {
                var offset = (wallMountComponent.Direction + xform.LocalRotation - Math.PI / 2).ToVec() * WallVendEjectDistanceFromWall;
                coordinates = coordinates.Offset(offset);
            }

            return coordinates;
        }

        /// <summary>
        /// Removes one stored returned item of the requested prototype.
        /// </summary>
        /// <returns>True if a real item was moved out of storage.</returns>
        private bool TryRemoveStoredReturnedItem(
            VendingMachineComponent component,
            string itemId,
            EntityCoordinates destination,
            bool updateInventory,
            out EntityUid item,
            out bool changed)
        {
            item = default;
            changed = CleanupStaleReturnedItems(component, itemId, updateInventory);

            if (component.ReturnedInventory is null ||
                !component.ReturnedInventory.TryGetValue(itemId, out var returned))
                return false;

            // Remove from the end so List<T> does not need to shift the remaining entries.
            for (var index = returned.Count - 1; index >= 0; index--)
            {
                item = returned[index];

                // Keep the lookup entry if storage removal fails, otherwise a later fallback spawn could duplicate stock.
                if (!_container.Remove(item, component.ReturnedInventoryContainer, destination: destination))
                    continue;

                returned.RemoveAt(index);
                changed = true;

                // No returned items of this type are left, so remove the entry from the lookup dictionary.
                if (returned.Count == 0)
                {
                    component.ReturnedInventory.Remove(itemId);

                    if (component.ReturnedInventory.Count == 0)
                        component.ReturnedInventory = null;
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Removes one stored returned item for vending.
        /// </summary>
        /// <returns>True if vending should use the returned entity instead of spawning a new one.</returns>
        private bool TryTakeReturnedItemForVend(VendingMachineComponent component, string itemId, EntityCoordinates spawnCoordinates, out EntityUid item)
        {
            return TryRemoveStoredReturnedItem(component, itemId, spawnCoordinates, false, out item, out _);
        }
    }
}
