// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Containers;

namespace Content.Shared.VendingMachines
{
    public sealed partial class VendingMachineComponent
    {
        /// <summary>
        /// Container ID used to store entities returned to this vending machine.
        /// </summary>
        /// <remarks>
        /// Kept as a constant so creation and removal always refer to the same container.
        /// </remarks>
        public const string ReturnedInventoryContainerId = "vending_machine_returned_inventory";

        /// <summary>
        /// Hidden container holding returned item entities.
        /// </summary>
        /// <remarks>
        /// Vending inventory tracks prototype IDs and counts, so returned entities need separate storage.
        /// </remarks>
        public Container ReturnedInventoryContainer = default!;

        /// <summary>
        /// Returned entities grouped by prototype ID.
        /// </summary>
        /// <remarks>
        /// Used to vend returned entities before falling back to spawning new prototype instances.
        /// A null value means no items have been returned to this machine yet.
        /// </remarks>
        public Dictionary<string, List<EntityUid>>? ReturnedInventory;
    }
}
