using Content.Server.Administration.Logs;
using Content.Server.Cargo.Systems;
using Content.Server.Station.Systems;
using Content.Shared._Aquila.Economy;
using Content.Shared._Aquila.Economy.Components;
using Content.Shared._Aquila.Economy.Systems;
using Content.Shared.Access.Systems;
using Content.Shared.Cargo.Components;
using Content.Shared.Database;
using Content.Shared.Popups;
using Content.Shared.VendingMachines;
using Robust.Shared.Audio.Systems;

namespace Content.Server._Aquila.Economy.Systems;

public sealed class PaidVendingMachineSystem : SharedPaidVendingMachineSystem
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly BankAccountSystem _bank = default!;
    [Dependency] private readonly CargoSystem _cargo = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedIdCardSystem _idCard = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedVendingMachineSystem _vending = default!;
    [Dependency] private readonly StationSystem _station = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PaidVendingMachineComponent, VendingMachinePurchaseAttemptEvent>(OnVendingMachinePurchaseAttempt);
    }

    private void OnVendingMachinePurchaseAttempt(Entity<PaidVendingMachineComponent> ent, ref VendingMachinePurchaseAttemptEvent args)
    {
        if (!CanBuy(ent.AsNullable(), args.User, args.ItemId, args.Type))
        {
            Refuse(ent, args.User, Loc.GetString("vending-paid-restricted"));
            args.Cancelled = true;
            return;
        }

        var price = GetPriceFor(ent.AsNullable(), args.User, args.ItemId);
        if (price <= 0)
            return;

        if (!_idCard.TryFindIdCard(args.User, out var idCard) || !_bank.TryGetAccount(idCard.Owner, out var account))
        {
            Refuse(ent, args.User, Loc.GetString("vending-paid-no-account"));
            args.Cancelled = true;
            return;
        }

        if (!_bank.TryWithdraw(account.Value.AsNullable(), price, ent.Comp.PurchaseReason, Name(ent)))
        {
            Refuse(ent, args.User, Loc.GetString("vending-paid-insufficient", ("price", price)));
            args.Cancelled = true;
            return;
        }

        if (_station.GetOwningStation(ent) is { } station && TryComp<StationBankAccountComponent>(station, out var stationBank))
            _cargo.UpdateBankAccount((station, stationBank), (int) (price * ent.Comp.RevenueShare), ent.Comp.RevenueAccount);

        if (TryComp<VendingMachineComponent>(ent, out var vending))
            _audio.PlayEntity(vending.SoundVend, args.User, ent);

        _audio.PlayEntity(ent.Comp.PurchaseSound, args.User, ent);
        _popup.PopupEntity(Loc.GetString("vending-paid-purchased", ("price", price)), ent, args.User);
        _adminLog.Add(LogType.Action, LogImpact.Low,
            $"{ToPrettyString(args.User):user} paid {price} credits for {args.ItemId} at {ToPrettyString(ent):vendor}");
    }

    private void Refuse(EntityUid vendor, EntityUid user, string message)
    {
        _popup.PopupEntity(message, vendor, user);
        _vending.Deny(vendor);
    }
}
