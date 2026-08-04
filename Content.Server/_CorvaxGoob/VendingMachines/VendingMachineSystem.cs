// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using Content.Server.Power.EntitySystems;
using Content.Shared.Destructible;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.VendingMachines;
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

        private void InitializeVendingReturn()
        {
            SubscribeLocalEvent<VendingMachineComponent, ComponentInit>(OnComponentInit);

            // Some closed Openable items handle fallback interactions before the vending machine can see them.
            // Use InteractUsingEvent so those items can still be returned before OpenableSystem blocks the click.
            SubscribeLocalEvent<VendingMachineComponent, InteractUsingEvent>(OnInteractUsing);

            SubscribeLocalEvent<VendingMachineComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<VendingMachineComponent, DestructionEventArgs>(OnDestruction);
            SubscribeLocalEvent<VendingMachineComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAlternativeVerbs);
        }

        private void OnComponentInit(Entity<VendingMachineComponent> ent, ref ComponentInit args)
        {
            // Returned items are real entities stored in the machine, while vending inventory only tracks prototype counts.
            ent.Comp.ReturnedInventoryContainer = _container.EnsureContainer<Container>(ent, VendingMachineComponent.ReturnedInventoryContainerId);
        }

        private void OnInteractUsing(EntityUid uid, VendingMachineComponent component, InteractUsingEvent args)
        {
            if (args.Handled)
                return;

            // Only consume the click when the item was actually accepted, preserving normal interactions otherwise.
            args.Handled = TryReturnItem((uid, component), args.User, args.Used);
        }

        private bool TryReturnItem(Entity<VendingMachineComponent> vending, EntityUid user, EntityUid used)
        {
            var (uid, component) = vending;

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

        private void OnExamined(EntityUid uid, VendingMachineComponent component, ExaminedEvent args)
        {
            if (!HasStoredReturnedItems(component))
                return;

            var message = Loc.GetString("vending-machine-component-returned-items-examine");
            args.PushMarkup($"[color=yellow]{message}[/color]");
        }

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

        private void OnDestruction(EntityUid uid, VendingMachineComponent component, DestructionEventArgs args)
        {
            // When the vending machine is destroyed, all previously returned items stored inside it fall out.
            TryEjectStoredReturnedItems(uid, component, updateInventory: false, out _);
        }

        private void OnGetAlternativeVerbs(EntityUid uid, VendingMachineComponent component, GetVerbsEvent<AlternativeVerb> args)
        {
            // Show the verb as long as there is something real to remove.
            // Panel, power, and access checks happen in Act so the user gets a specific failure popup.
            if (!args.CanInteract || !HasStoredReturnedItems(component))
                return;

            var verb = new AlternativeVerb
            {
                Text = Loc.GetString("vending-machine-component-remove-returned-items"),
                Act = () => RemoveReturnedItems(uid, args.User),
                Priority = 2,
            };

            args.Verbs.Add(verb);
        }

        private void RemoveReturnedItems(EntityUid uid, EntityUid user)
        {
            if (!TryComp<VendingMachineComponent>(uid, out var component))
                return;

            // The menu hides empty machines, but returned items can be removed or deleted before the click arrives.
            if (!HasStoredReturnedItems(component))
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
                Deny((uid, component), user);

                // Keep the manual removal feedback consistent with normal unauthorized vending attempts.
                Audio.PlayEntity(component.SoundDeny, user, uid, AudioParams.Default.WithVolume(-2f));
                return;
            }

            var ejectedAny = TryEjectStoredReturnedItems(uid, component, updateInventory: true, out var changed);
            if (!ejectedAny)
            {
                // Stale returned entries may be cleaned up without any real item being ejected.
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

        private bool TryEjectStoredReturnedItems(EntityUid uid, VendingMachineComponent component, bool updateInventory, out bool changed)
        {
            changed = false;

            if (component.ReturnedInventory is null)
                return false;

            var ejectedAny = false;
            var coordinates = Transform(uid).Coordinates;
            var returnedInventory = component.ReturnedInventory;

            // Buckets are removed while iterating, so walk a snapshot of the prototype keys.
            foreach (var itemId in new List<string>(returnedInventory.Keys))
            {
                if (!returnedInventory.TryGetValue(itemId, out var returned))
                    continue;

                while (returned.Count > 0)
                {
                    var index = returned.Count - 1;
                    var item = returned[index];
                    returned.RemoveAt(index);
                    changed = true;

                    // Only real entities still stored inside this machine should drop or affect visible stock.
                    if (Deleted(item) || !_container.Remove(item, component.ReturnedInventoryContainer, destination: coordinates))
                        continue;

                    ejectedAny = true;

                    // Manual removal must undo the stock increase that happened when the item was returned.
                    // Destruction skips this because the vending machine and its inventory are being deleted anyway.
                    if (updateInventory &&
                        TryGetReturnableEntry(component, itemId, out var entry) &&
                        entry.Amount > 0)
                        entry.Amount--;
                }

                if (returnedInventory.Remove(itemId))
                    changed = true;
            }

            if (returnedInventory.Count == 0)
            {
                component.ReturnedInventory = null;
                changed = true;
            }

            return ejectedAny;
        }

        /// <summary>
        /// Tries to take a previously returned item from the vending machine storage for vending.
        /// </summary>
        /// <remarks>
        /// Returned items are stored as real entities so their current state is preserved.
        /// This method removes one stored entity matching <paramref name="itemId"/> from the hidden
        /// returned-items container and moves it to <paramref name="spawnCoordinates"/>.
        /// If no valid stored entity is available, the caller should fall back to spawning a new prototype.
        /// </remarks>
        /// <param name="component">The vending machine component that owns the returned-items storage.</param>
        /// <param name="itemId">The prototype ID of the item that should be vended.</param>
        /// <param name="spawnCoordinates">The coordinates where the stored item should be moved.</param>
        /// <param name="item">The stored returned entity, if one was found and moved successfully.</param>
        /// <returns>
        /// True if a stored returned entity was moved out of storage and should be vended;
        /// false if the caller should spawn a new item normally.
        /// </returns>
        private bool TryTakeReturnedItemForVend(VendingMachineComponent component, string itemId, EntityCoordinates spawnCoordinates, out EntityUid item)
        {
            item = default;

            // No returned entity is stored for this prototype, so the caller should spawn normally.
            if (component.ReturnedInventory is null ||
                !component.ReturnedInventory.TryGetValue(itemId, out var returned))
                return false;

            // Remove from the end so List<T> does not need to shift the remaining entries.
            while (returned.Count > 0)
            {
                var index = returned.Count - 1;
                item = returned[index];
                returned.RemoveAt(index);

                // Skip stale entries if the stored entity was deleted or cannot be removed from storage.
                if (Deleted(item) || !_container.Remove(item, component.ReturnedInventoryContainer, destination: spawnCoordinates))
                    continue;

                // No returned items of this type are left, so remove the entry from the lookup dictionary.
                if (returned.Count == 0)
                    component.ReturnedInventory.Remove(itemId);

                return true;
            }

            // None of the saved returned items could be used, so remove this type from the lookup dictionary.
            component.ReturnedInventory.Remove(itemId);
            return false;
        }
    }
}
