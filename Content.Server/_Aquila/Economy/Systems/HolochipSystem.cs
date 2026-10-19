using Content.Server._Aquila.Economy.Components;
using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Server.Stack;
using Content.Shared._Aquila.Economy.Systems;
using Content.Shared.Access.Systems;
using Content.Shared.Database;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.PDA;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;

namespace Content.Server._Aquila.Economy.Systems;

public sealed class HolochipSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly BankAccountSystem _bank = default!;
    [Dependency] private readonly EconomyZoneSystem _economyZone = default!;
    [Dependency] private readonly QuickDialogSystem _quickDialog = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedIdCardSystem _idCard = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StackSystem _stack = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BankCardComponent, GetVerbsEvent<AlternativeVerb>>(OnCardGetVerbs);
        SubscribeLocalEvent<HolochipComponent, AfterInteractEvent>(OnChipAfterInteract);
        SubscribeLocalEvent<HolochipComponent, UseInHandEvent>(OnChipUseInHand);
    }

    private void OnCardGetVerbs(Entity<BankCardComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        AddWithdrawVerb(args, ent);
    }

    private void OnChipAfterInteract(Entity<HolochipComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target || GetCard(target) is not { } card)
            return;

        args.Handled = true;
        Deposit(ent, card, args.User);
    }

    private void OnChipUseInHand(Entity<HolochipComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled
            || !_idCard.TryFindIdCard(args.User, out var idCard)
            || !TryComp<BankCardComponent>(idCard, out var card))
            return;

        args.Handled = true;
        Deposit(ent, (idCard.Owner, card), args.User);
    }

    private Entity<BankCardComponent>? GetCard(EntityUid target)
    {
        if (TryComp<BankCardComponent>(target, out var card))
            return (target, card);

        if (TryComp<PdaComponent>(target, out var pda) && pda.ContainedId is { } id && TryComp<BankCardComponent>(id, out var pdaCard))
            return (id, pdaCard);

        return null;
    }

    private void Deposit(Entity<HolochipComponent> chip, Entity<BankCardComponent> card, EntityUid user)
    {
        if (!_economyZone.IsEconomyActive(user))
        {
            _popup.PopupEntity(Loc.GetString(_economyZone.IsEconomyEnabled() ? "holochip-free-zone" : "bank-offline"), user, user);
            return;
        }

        if (!_bank.TryGetAccount(card.AsNullable(), out var account))
        {
            _popup.PopupEntity(Loc.GetString("bank-ui-no-account"), user, user);
            return;
        }

        if (!TryComp<StackComponent>(chip, out var stack))
            return;

        var amount = stack.Count;
        _bank.Deposit(account.Value.AsNullable(), amount, chip.Comp.DepositReason);
        QueueDel(chip);

        _audio.PlayPvs(chip.Comp.DepositSound, user);
        _popup.PopupEntity(Loc.GetString("holochip-deposited", ("amount", amount)), user, user);

        var number = account.Value.Comp.Number;
        _adminLog.Add(LogType.Action, LogImpact.Low,
            $"{ToPrettyString(user):user} deposited a holochip of {amount} credits to account {number}");
    }

    private void AddWithdrawVerb(GetVerbsEvent<AlternativeVerb> args, Entity<BankCardComponent> card)
    {
        if (!args.CanAccess
            || !args.CanInteract
            || !_economyZone.IsEconomyEnabled()
            || !_bank.TryGetAccount(card.AsNullable(), out _)
            || !TryComp<ActorComponent>(args.User, out var actor))
            return;

        var user = args.User;
        var session = actor.PlayerSession;
        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("holochip-verb-withdraw"),
            Icon = card.Comp.ChipVerbIcon,
            Priority = card.Comp.ChipVerbPriority,
            Act = () => _quickDialog.OpenDialog<int>(session,
                Loc.GetString("holochip-verb-withdraw"),
                Loc.GetString("holochip-dialog-amount"),
                amount => Withdraw(card, user, amount)),
        });
    }

    private void Withdraw(Entity<BankCardComponent> card, EntityUid user, int amount)
    {
        if (TerminatingOrDeleted(card) || TerminatingOrDeleted(user) || !_interaction.InRangeUnobstructed(user, card.Owner))
            return;

        if (!_economyZone.IsEconomyActive(user))
        {
            _popup.PopupEntity(Loc.GetString(_economyZone.IsEconomyEnabled() ? "holochip-free-zone" : "bank-offline"), user, user);
            return;
        }

        if (!_bank.TryGetAccount(card.AsNullable(), out var account))
            return;

        if (amount <= 0 || !_bank.TryWithdraw(account.Value.AsNullable(), amount, card.Comp.ChipWithdrawReason))
        {
            _popup.PopupEntity(Loc.GetString("bank-transfer-insufficient"), user, user);
            return;
        }

        foreach (var chip in _stack.SpawnMultipleAtPosition(card.Comp.ChipStack, amount, Transform(user).Coordinates))
        {
            _hands.PickupOrDrop(user, chip);
        }

        _audio.PlayPvs(card.Comp.ChipSound, user);
        _popup.PopupEntity(Loc.GetString("holochip-withdrawn", ("amount", amount)), user, user);

        var number = account.Value.Comp.Number;
        _adminLog.Add(LogType.Action, LogImpact.Low,
            $"{ToPrettyString(user):user} withdrew {amount} credits as holochips from account {number}");
    }
}
