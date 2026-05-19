using System.Linq;
using System.Numerics;
using Robust.Shared.Random;
namespace Content.Shared.VendingMachines;

public abstract partial class SharedVendingMachineSystem
{
    private void InitializeVending()
    {
        SubscribeLocalEvent<VendingMachineComponent, VendingMachineSelfDispenseTargetEvent>(OnSelfDispense);
    }

    private void OnSelfDispense(EntityUid uid, VendingMachineComponent component, VendingMachineSelfDispenseTargetEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        component.TargetDirection = args.Target;
        EjectRandomTarget(uid, throwItem: true, forceEject: false, component);
    }
    public void EjectRandomTarget(EntityUid uid, bool throwItem, bool forceEject = false, VendingMachineComponent? vendComponent = null)
    {
        if (!Resolve(uid, ref vendComponent))
            return;

        var availableItems = GetAvailableInventory(uid, vendComponent);
        if (availableItems.Count <= 0)
            return;

        var item = Randomizer.Pick(availableItems);
        if (forceEject)
        {
            vendComponent.NextItemToEject = item.ID;
            vendComponent.ThrowNextItem = throwItem;
            var entry = GetEntry(uid, item.ID, item.Type, vendComponent);
            if (entry != null)
                entry.Amount--;
            EjectItem(uid, vendComponent, forceEject);
        }
        else
        {
            TryEjectVendorItem(uid, item.Type, item.ID, throwItem, user: null, vendComponent: vendComponent);
        }
    }

}
