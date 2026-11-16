using Content.Server.Cargo.Components;
using Content.Shared._BlackM.Cargo;
using Content.Shared.Cargo;
using Content.Shared.Cargo.Components;
using Robust.Shared.Map;

// Partial class must stay in the CargoSystem namespace.
namespace Content.Server.Cargo.Systems;

public sealed partial class CargoSystem
{
    public readonly record struct LiftExportLot(EntityUid Item, HashSet<EntityUid> Snapshot, double Quoted);

    public (EntityUid Item, EntityUid Invoice) LiftSpawnDelivery(CargoOrderData order, EntityCoordinates at)
    {
        FulfillOrder(order, order.Account, at, "PaperCargoInvoice", out var item, out var invoice);
        return (item, invoice);
    }

    public bool LiftCanExport(EntityUid uid, out double price)
    {
        price = 0;
        if (TerminatingOrDeleted(uid) || HasComp<CargoTransitComponent>(uid) ||
            !TryComp<TransformComponent>(uid, out var xform) || xform.Anchored ||
            xform.ParentUid != xform.GridUid || !LiftExportTreeSafe(uid))
            return false;
        price = _pricing.GetPrice(uid);
        return double.IsFinite(price) && price > 0 && price <= int.MaxValue;
    }

    public EntityUid? LiftFindForbidden(EntityUid uid)
    {
        if (TerminatingOrDeleted(uid))
            return null;
        if (_mobQuery.HasComponent(uid) || _blacklistQuery.HasComponent(uid))
            return uid;
        var children = Transform(uid).ChildEnumerator;
        while (children.MoveNext(out var child))
        {
            if (LiftFindForbidden(child) is { } found)
                return found;
        }
        return null;
    }

    // Recursive blacklist, including nested containers.
    private bool LiftExportTreeSafe(EntityUid uid, bool allowReservation = false)
    {
        if (_mobQuery.HasComponent(uid) || _blacklistQuery.HasComponent(uid) ||
            (!allowReservation && HasComp<CargoTransitComponent>(uid)))
            return false;
        var children = Transform(uid).ChildEnumerator;
        while (children.MoveNext(out var child))
        {
            if (!LiftExportTreeSafe(child))
                return false;
        }
        return true;
    }

    private void LiftCollectTree(EntityUid uid, HashSet<EntityUid> into)
    {
        if (!into.Add(uid))
            return;
        var children = Transform(uid).ChildEnumerator;
        while (children.MoveNext(out var child))
            LiftCollectTree(child, into);
    }

    // Pays only if contents match the snapshot and never more than the quoted price.
    public int LiftCommitExports(IReadOnlyList<LiftExportLot> lots, EntityUid station, HashSet<EntityUid> sold)
    {
        sold.Clear();
        if (TerminatingOrDeleted(station) || !TryComp<StationBankAccountComponent>(station, out var bank))
            return 0;
        double total = 0;
        var current = new HashSet<EntityUid>();
        foreach (var lot in lots)
        {
            if (TerminatingOrDeleted(lot.Item) || _station.GetOwningStation(lot.Item) != station ||
                !LiftExportTreeSafe(lot.Item, true))
                continue;
            current.Clear();
            LiftCollectTree(lot.Item, current);
            if (!current.SetEquals(lot.Snapshot))
            {
                Log.Warning($"Cargo lift: package {ToPrettyString(lot.Item)} was tampered with during export, sale refused.");
                continue;
            }
            var price = _pricing.GetPrice(lot.Item);
            if (!double.IsFinite(price) || price <= 0)
                continue;
            price = Math.Min(price, lot.Quoted);
            if (total + price > int.MaxValue)
                continue;
            total += price;
            sold.Add(lot.Item);
        }
        if (sold.Count == 0)
            return 0;
        var ev = new EntitySoldEvent(new HashSet<EntityUid>(sold), station);
        RaiseLocalEvent(ref ev);
        foreach (var uid in sold)
            Del(uid);
        var amount = (int) Math.Round(total);
        UpdateBankAccount((station, bank), amount, bank.PrimaryAccount, false);
        Dirty(station, bank);
        Log.Info($"Cargo lift exported {sold.Count} packages for {amount} at station {station}.");
        return amount;
    }

    public bool LiftCommitExport(EntityUid uid, EntityUid station)
    {
        if (TerminatingOrDeleted(uid))
            return false;
        var snapshot = new HashSet<EntityUid>();
        LiftCollectTree(uid, snapshot);
        var sold = new HashSet<EntityUid>();
        return LiftCommitExports(new[] { new LiftExportLot(uid, snapshot, double.MaxValue) }, station, sold) > 0;
    }

    public void LiftRefund(EntityUid station, CargoOrderData order, int count)
    {
        if (count <= 0 || !TryComp<StationBankAccountComponent>(station, out var bank))
            return;
        UpdateBankAccount((station, bank), order.Price * count, order.Account, false);
        Dirty(station, bank);
        Log.Info($"Cargo lift refunded {count} units of order {order.OrderId} at station {station}.");
    }
}
