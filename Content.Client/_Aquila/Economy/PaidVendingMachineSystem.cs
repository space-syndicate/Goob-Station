using Content.Shared._Aquila.Economy;
using Content.Shared._Aquila.Economy.Components;
using Content.Shared._Aquila.Economy.Systems;

namespace Content.Client._Aquila.Economy;

public sealed class PaidVendingMachineSystem : SharedPaidVendingMachineSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PaidVendingMachineComponent, VendingMachinePurchaseAttemptEvent>(OnVendingMachinePurchaseAttempt);
    }

    private void OnVendingMachinePurchaseAttempt(Entity<PaidVendingMachineComponent> ent, ref VendingMachinePurchaseAttemptEvent args)
    {
        if (!CanBuy(ent.AsNullable(), args.User, args.ItemId, args.Type)
            || GetPriceFor(ent.AsNullable(), args.User, args.ItemId) > 0)
            args.Cancelled = true;
    }
}
