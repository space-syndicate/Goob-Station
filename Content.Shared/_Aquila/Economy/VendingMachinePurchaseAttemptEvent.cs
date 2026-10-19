using Content.Shared.VendingMachines;

namespace Content.Shared._Aquila.Economy;

[ByRefEvent]
public record struct VendingMachinePurchaseAttemptEvent(EntityUid User, string ItemId, InventoryType Type, bool Cancelled = false);
