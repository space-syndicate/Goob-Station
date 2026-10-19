using System.Diagnostics.CodeAnalysis;
using Content.Server._Aquila.Economy.Components;
using Content.Server.Administration.Logs;
using Content.Server.Stack;
using Content.Shared._Aquila.Economy;
using Content.Shared._Aquila.Economy.Systems;
using Content.Shared.Access.Systems;
using Content.Shared.Database;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Robust.Shared.Audio.Systems;

namespace Content.Server._Aquila.Economy.Systems;

public sealed class AtmSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly BankAccountSystem _bank = default!;
    [Dependency] private readonly EconomyZoneSystem _economyZone = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedIdCardSystem _idCard = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly StackSystem _stack = default!;

    public override void Initialize()
    {
        base.Initialize();

        Subs.BuiEvents<AtmComponent>(AtmUiKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnBoundUIOpened);
            subs.Event<AtmWithdrawMessage>(OnAtmWithdraw);
            subs.Event<AtmOpenAccountMessage>(OnAtmOpenAccount);
        });

        SubscribeLocalEvent<AtmComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnBoundUIOpened(Entity<AtmComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUiState(ent, args.Actor);
    }

    private void OnAtmOpenAccount(Entity<AtmComponent> ent, ref AtmOpenAccountMessage args)
    {
        if (!_economyZone.IsEconomyEnabled())
        {
            Refuse(ent, args.Actor, Loc.GetString("bank-offline"));
            return;
        }

        if (!_idCard.TryFindIdCard(args.Actor, out var idCard) || !_bank.TryOpenAccount(idCard.Owner, Name(args.Actor), out var account))
        {
            Refuse(ent, args.Actor, Loc.GetString("bank-open-account-failed"));
            return;
        }

        var number = account.Value.Comp.Number;
        _popup.PopupEntity(Loc.GetString("bank-open-account-success", ("number", number.ToString())), ent, args.Actor);
        _adminLog.Add(LogType.Action, LogImpact.Low,
            $"{ToPrettyString(args.Actor):user} opened bank account {number} for {ToPrettyString(idCard):card} at {ToPrettyString(ent):atm}");
        UpdateUiState(ent, args.Actor);
    }

    private void OnAtmWithdraw(Entity<AtmComponent> ent, ref AtmWithdrawMessage args)
    {
        if (!CheckActive(ent, args.Actor))
            return;

        if (!TryGetUserAccount(args.Actor, out var account))
        {
            Refuse(ent, args.Actor, Loc.GetString("atm-no-account"));
            return;
        }

        if (args.Amount <= 0 || !_bank.TryWithdraw(account.Value.AsNullable(), args.Amount, ent.Comp.WithdrawReason))
        {
            Refuse(ent, args.Actor, Loc.GetString("bank-transfer-insufficient"));
            UpdateUiState(ent, args.Actor);
            return;
        }

        foreach (var cash in _stack.SpawnMultipleAtPosition(ent.Comp.CashStack, args.Amount, Transform(args.Actor).Coordinates))
        {
            _hands.PickupOrDrop(args.Actor, cash);
        }

        _audio.PlayPvs(ent.Comp.WithdrawSound, ent);

        var number = account.Value.Comp.Number;
        _adminLog.Add(LogType.Action, LogImpact.Low,
            $"{ToPrettyString(args.Actor):user} withdrew {args.Amount} credits from account {number} at {ToPrettyString(ent):atm}");
        UpdateUiState(ent, args.Actor);
    }

    private void OnInteractUsing(Entity<AtmComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled
            || !TryComp<StackComponent>(args.Used, out var stack)
            || stack.StackTypeId != ent.Comp.CashStack)
            return;

        args.Handled = true;

        if (!CheckActive(ent, args.User))
            return;

        if (!TryGetUserAccount(args.User, out var account))
        {
            Refuse(ent, args.User, Loc.GetString("atm-no-account"));
            return;
        }

        var amount = stack.Count;
        _bank.Deposit(account.Value.AsNullable(), amount, ent.Comp.DepositReason);
        QueueDel(args.Used);

        _audio.PlayPvs(ent.Comp.DepositSound, ent);
        _popup.PopupEntity(Loc.GetString("atm-deposited", ("amount", amount)), ent, args.User);

        var number = account.Value.Comp.Number;
        _adminLog.Add(LogType.Action, LogImpact.Low,
            $"{ToPrettyString(args.User):user} deposited {amount} credits to account {number} at {ToPrettyString(ent):atm}");

        if (_ui.IsUiOpen(ent.Owner, AtmUiKey.Key, args.User))
            UpdateUiState(ent, args.User);
    }

    private bool CheckActive(Entity<AtmComponent> ent, EntityUid user)
    {
        if (_economyZone.IsEconomyActive(ent))
            return true;

        Refuse(ent, user, Loc.GetString(_economyZone.IsEconomyEnabled() ? "atm-free-zone" : "bank-offline"));
        return false;
    }

    private void Refuse(Entity<AtmComponent> ent, EntityUid user, string message)
    {
        _popup.PopupEntity(message, ent, user);
        _audio.PlayPvs(ent.Comp.DenySound, ent);
    }

    private void UpdateUiState(Entity<AtmComponent> ent, EntityUid user)
    {
        if (!_economyZone.IsEconomyEnabled())
        {
            _ui.SetUiState(ent.Owner, AtmUiKey.Key, new AtmUiState(null, null, 0, offline: true));
            return;
        }

        var state = TryGetUserAccount(user, out var account)
            ? new AtmUiState(account.Value.Comp.Number, account.Value.Comp.OwnerName, account.Value.Comp.Balance)
            : new AtmUiState(null, null, 0, _idCard.TryFindIdCard(user, out var idCard) && _bank.CanOpenAccount(idCard.Owner));

        _ui.SetUiState(ent.Owner, AtmUiKey.Key, state);
    }

    private bool TryGetUserAccount(EntityUid user, [NotNullWhen(true)] out Entity<BankAccountComponent>? account)
    {
        account = null;
        return _idCard.TryFindIdCard(user, out var idCard) && _bank.TryGetAccount(idCard.Owner, out account);
    }
}
