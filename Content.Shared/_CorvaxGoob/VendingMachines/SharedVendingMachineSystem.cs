// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameObjects;

namespace Content.Shared.VendingMachines;

public abstract partial class SharedVendingMachineSystem
{
    /// <summary>
    /// Synchronizes stale returned-item stock before item availability checks.
    /// </summary>
    /// <returns>True if vending state changed.</returns>
    protected virtual bool CleanupReturnedInventoryBeforeVend(
        EntityUid uid,
        VendingMachineComponent component,
        string itemId)
    {
        return false;
    }
}
