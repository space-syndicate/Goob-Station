using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server._Aquila.Economy.Components;
using Content.Server._Aquila.Economy.DataDefinitions;
using Content.Server.Administration.Logs;
using Content.Server.Cargo.Systems;
using Content.Server.CartridgeLoader;
using Content.Server.Radio.EntitySystems;
using Content.Server.Station.Systems;
using Content.Shared._Aquila.Economy;
using Content.Shared._Aquila.Economy.DataDefinitions;
using Content.Shared._Aquila.Economy.Systems;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Cargo.Components;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.CartridgeLoader;
using Content.Shared.Database;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.PDA;
using Content.Shared.Popups;
using Content.Shared.Radio;
using Content.Shared.Roles;
using Robust.Shared.Player;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Aquila.Economy.Systems;

public sealed class BankCartridgeSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly BankAccountSystem _bank = default!;
    [Dependency] private readonly CargoSystem _cargo = default!;
    [Dependency] private readonly CartridgeLoaderSystem _cartridgeLoader = default!;
    [Dependency] private readonly EconomyZoneSystem _economyZone = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedAccessSystem _access = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedIdCardSystem _idCard = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StationSystem _station = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BankCartridgeComponent, CartridgeUiReadyEvent>(OnCartridgeUiReady);
        SubscribeLocalEvent<BankCartridgeComponent, CartridgeMessageEvent>(OnCartridgeMessage);
        SubscribeLocalEvent<BankCardComponent, BankTransactionEvent>(OnBankTransaction);
        SubscribeLocalEvent<IdCardComponent, EntGotInsertedIntoContainerMessage>(OnIdCardInserted);
        SubscribeLocalEvent<IdCardComponent, EntGotRemovedFromContainerMessage>(OnIdCardRemoved);
    }

    private void OnCartridgeUiReady(Entity<BankCartridgeComponent> ent, ref CartridgeUiReadyEvent args)
    {
        UpdateUiState(ent, args.Loader);
    }

    private void OnCartridgeMessage(Entity<BankCartridgeComponent> ent, ref CartridgeMessageEvent args)
    {
        var loader = GetEntity(args.LoaderUid);
        if (!_economyZone.IsEconomyEnabled())
        {
            Refuse(ent, Loc.GetString("bank-offline"), loader, args.Actor);
            UpdateUiState(ent, loader);
            return;
        }

        switch (args)
        {
            case BankOpenAccountMessageEvent:
                OpenAccount(ent, loader, args.Actor);
                UpdateUiState(ent, loader);
                return;
            case BankTransferMessageEvent transfer:
                Transfer(ent, loader, args.Actor, transfer);
                UpdateUiState(ent, loader);
                return;
            case BankBonusMessageEvent bonus:
                Bonus(ent, loader, args.Actor, bonus);
                break;
            case BankFineMessageEvent fine:
                Fine(ent, loader, args.Actor, fine);
                break;
            case BankRequestReplyMessageEvent reply:
                Reply(ent, loader, args.Actor, reply);
                break;
            case BankFineRevokeMessageEvent revoke:
                Revoke(ent, loader, args.Actor, revoke);
                break;
            default:
                return;
        }

        UpdateAllUiStates();
    }

    private void OnIdCardInserted(Entity<IdCardComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        UpdateActiveProgram(args.Container.Owner);
    }

    private void OnIdCardRemoved(Entity<IdCardComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        UpdateActiveProgram(args.Container.Owner);
    }

    private void UpdateActiveProgram(EntityUid pda)
    {
        if (!TryComp<CartridgeLoaderComponent>(pda, out var loader)
            || !_cartridgeLoader.TryGetProgram<BankCartridgeComponent>(pda, out var program, out var cartridge, loader: loader)
            || loader.ActiveProgram != program)
            return;

        UpdateUiState((program.Value, cartridge), pda);
    }

    private void OnBankTransaction(Entity<BankCardComponent> ent, ref BankTransactionEvent args)
    {
        if (!TryGetCardLoader(ent, out var pda, out var loader)
            || !_cartridgeLoader.TryGetProgram<BankCartridgeComponent>(pda.Value, out var program, out var cartridge, loader: loader))
            return;

        var transaction = args.Transaction;
        if (transaction.Amount > 0 || transaction.Comment != null)
        {
            var reason = Loc.GetString(transaction.Reason, ("party", transaction.Party ?? string.Empty));
            if (transaction.Comment != null)
                reason = Loc.GetString("bank-transaction-with-comment", ("reason", reason), ("comment", transaction.Comment));

            _cartridgeLoader.SendNotification(pda.Value,
                Loc.GetString("bank-program-name"),
                Loc.GetString(transaction.Amount > 0 ? "bank-notification-income" : "bank-notification-charge",
                    ("amount", Math.Abs(transaction.Amount)),
                    ("reason", reason)),
                loader);
        }

        if (transaction.Amount > 0 && !cartridge.SilentIncomeReasons.Contains(transaction.Reason) && _container.TryGetContainingContainer((pda.Value, null, null), out var holder))
            PlaySound((program.Value, cartridge), cartridge.IncomeSound, holder.Owner, pda.Value);

        if (loader.ActiveProgram == program)
            UpdateUiState((program.Value, cartridge), pda.Value);
    }

    private void OpenAccount(Entity<BankCartridgeComponent> ent, EntityUid loader, EntityUid user)
    {
        if (!TryComp<PdaComponent>(loader, out var pda)
            || pda.ContainedId is not { } id
            || !_bank.TryOpenAccount(id, Name(user), out var account))
        {
            Refuse(ent, Loc.GetString("bank-open-account-failed"), loader, user);
            return;
        }

        var number = account.Value.Comp.Number;
        Succeed(ent, Loc.GetString("bank-open-account-success", ("number", number.ToString())), loader, user);
        _adminLog.Add(LogType.Action, LogImpact.Low,
            $"{ToPrettyString(user):user} opened bank account {number} for {ToPrettyString(id):card}");
    }

    private void Transfer(Entity<BankCartridgeComponent> ent, EntityUid loader, EntityUid user, BankTransferMessageEvent transfer)
    {
        if (!TryGetLoaderAccount(loader, out _, out var from))
        {
            Refuse(ent, Loc.GetString("bank-transfer-no-card"), loader, user);
            return;
        }

        if (transfer.Amount <= 0)
        {
            Refuse(ent, Loc.GetString("bank-transfer-invalid-amount"), loader, user);
            return;
        }

        if (!_bank.TryFindAccount(transfer.Number, out var to) || to.Value.Owner == from.Value.Owner)
        {
            Refuse(ent, Loc.GetString("bank-transfer-invalid-account"), loader, user);
            return;
        }

        if (!_bank.TryTransfer(from.Value.AsNullable(), to.Value.AsNullable(), transfer.Amount))
        {
            Refuse(ent, Loc.GetString("bank-transfer-insufficient"), loader, user);
            return;
        }

        Succeed(ent, Loc.GetString("bank-transfer-success", ("amount", transfer.Amount), ("name", to.Value.Comp.OwnerName)), loader, user);
        _adminLog.Add(LogType.Action, LogImpact.Low,
            $"{ToPrettyString(user):user} transferred {transfer.Amount} credits from account {from.Value.Comp.Number} to account {to.Value.Comp.Number}");
    }

    private void Bonus(Entity<BankCartridgeComponent> ent, EntityUid loader, EntityUid user, BankBonusMessageEvent bonus)
    {
        if (!TryGetManageContext(ent, loader, user, bonus.Number, bonus.Amount, bonus.Comment, out var context))
            return;

        var (issuer, target, authority, station, management, comment) = context.Value;
        var record = CreateRecord(false, issuer, target, authority.Budget, bonus.Amount, comment);

        if (station.Comp.Accounts.GetValueOrDefault(authority.Budget) < bonus.Amount)
        {
            Refuse(ent, Loc.GetString("bank-manage-no-budget"), loader, user);
            return;
        }

        if (!_bank.TryReserveBonus(issuer.AsNullable(), bonus.Amount, authority.BonusLimit) && !IsApprover(ent.Comp, loader))
        {
            RequestApproval(management, loader, record);
            Succeed(ent, Loc.GetString("bank-manage-request-sent"), loader, user);
            return;
        }

        PayBonus(management, station, loader, record, null);
        Succeed(ent, Loc.GetString("bank-manage-bonus-success", ("amount", bonus.Amount), ("name", target.Comp.OwnerName)), loader, user);
    }

    private void Fine(Entity<BankCartridgeComponent> ent, EntityUid loader, EntityUid user, BankFineMessageEvent fine)
    {
        if (!TryGetManageContext(ent, loader, user, fine.Number, fine.Amount, fine.Comment, out var context))
            return;

        var (issuer, target, authority, station, management, comment) = context.Value;
        var record = CreateRecord(true, issuer, target, authority.Budget, fine.Amount, comment);

        if (!authority.UnlimitedFine && fine.Amount > authority.MaxFine && !IsApprover(ent.Comp, loader))
        {
            RequestApproval(management, loader, record);
            Succeed(ent, Loc.GetString("bank-manage-request-sent"), loader, user);
            return;
        }

        ApplyFine(management, station, loader, record, null);
        Succeed(ent, Loc.GetString("bank-manage-fine-success", ("amount", fine.Amount), ("name", target.Comp.OwnerName)), loader, user);
    }

    private void Reply(Entity<BankCartridgeComponent> ent, EntityUid loader, EntityUid user, BankRequestReplyMessageEvent reply)
    {
        if (!TryGetManager(ent, loader, user, _ => true, out var manager, out var station))
            return;

        if (!IsApprover(ent.Comp, loader))
        {
            Refuse(ent, GetApproverJob(ent.Comp) is { } approver && _proto.Resolve(approver, out var approverProto)
                ? Loc.GetString("bank-manage-not-approver", ("job", approverProto.LocalizedName))
                : Loc.GetString("bank-manage-no-approver"), loader, user);
            return;
        }

        var resolved = TryResolveRequest(station, reply.Id, reply.Approve, manager.Value.Comp.OwnerName, loader, out var message);
        if (resolved)
            Succeed(ent, Loc.GetString(message), loader, user);
        else
            Refuse(ent, Loc.GetString(message), loader, user);
    }

    private void Revoke(Entity<BankCartridgeComponent> ent, EntityUid loader, EntityUid user, BankFineRevokeMessageEvent revoke)
    {
        if (!TryGetManager(ent, loader, user, authority => authority.RevokeFines, out var manager, out var station))
            return;

        var revoked = TryRevokeFine(station, revoke.Id, manager.Value.Comp.OwnerName, loader, out var message);
        if (revoked)
            Succeed(ent, Loc.GetString(message), loader, user);
        else
            Refuse(ent, Loc.GetString(message), loader, user);
    }

    /// <summary>
    /// Подтверждает или отклоняет запрос на премию сверх лимита или штраф больше потолка.
    /// </summary>
    public bool TryResolveRequest(Entity<StationBankManagementComponent?> station, int id, bool approve, string managerName, EntityUid? source, out LocId message)
    {
        message = "bank-manage-record-missing";
        if (!Resolve(station, ref station.Comp, false)
            || !TryComp<StationBankAccountComponent>(station, out var stationBank))
            return false;

        var management = new Entity<StationBankManagementComponent>(station.Owner, station.Comp);
        var record = station.Comp.Requests.Find(request => request.Id == id);
        if (record == null)
            return false;

        if (!approve || !HasComp<BankAccountComponent>(record.Target))
        {
            station.Comp.Requests.Remove(record);
            Announce(source, station.Comp.RequestChannel, Loc.GetString(record.Fine ? "bank-radio-request-rejected-fine" : "bank-radio-request-rejected-bonus",
                ("amount", record.Amount),
                ("target", record.TargetName),
                ("issuer", record.IssuerName),
                ("manager", managerName)));
            Notify(record.Issuer, Loc.GetString("bank-notification-request-rejected", ("amount", record.Amount), ("target", record.TargetName)));
            _adminLog.Add(LogType.Action, LogImpact.Medium,
                $"{managerName} rejected request {record.Id} from {record.IssuerName}: {(record.Fine ? "fine" : "bonus")} of {record.Amount} credits for {record.TargetName}");
            message = "bank-manage-request-rejected";
            return true;
        }

        if (!record.Fine && stationBank.Accounts.GetValueOrDefault(record.Budget) < record.Amount)
        {
            message = "bank-manage-no-budget";
            return false;
        }

        station.Comp.Requests.Remove(record);
        if (record.Fine)
            ApplyFine(management, (station.Owner, stationBank), source, record, managerName);
        else
            PayBonus(management, (station.Owner, stationBank), source, record, managerName);

        Notify(record.Issuer, Loc.GetString("bank-notification-request-approved", ("amount", record.Amount), ("target", record.TargetName)));
        message = "bank-manage-request-approved";
        return true;
    }

    /// <summary>
    /// Отменяет выписанный штраф и возвращает деньги нарушителю из бюджета, куда ушёл штраф.
    /// </summary>
    public bool TryRevokeFine(Entity<StationBankManagementComponent?> station, int id, string managerName, EntityUid? source, out LocId message)
    {
        message = "bank-manage-record-missing";
        if (!Resolve(station, ref station.Comp, false)
            || !TryComp<StationBankAccountComponent>(station, out var stationBank))
            return false;

        var record = station.Comp.Fines.Find(fine => fine.Id == id);
        if (record == null)
            return false;

        if (stationBank.Accounts.GetValueOrDefault(record.Budget) < record.Amount)
        {
            message = "bank-manage-no-budget";
            return false;
        }

        station.Comp.Fines.Remove(record);
        _cargo.UpdateBankAccount((station.Owner, stationBank), -record.Amount, record.Budget);
        if (TryComp<BankAccountComponent>(record.Target, out var target))
            _bank.Deposit((record.Target, target), record.Amount, station.Comp.FineRevokedReason, managerName, record.Comment);

        Announce(source, GetBudgetChannel(record.Budget), Loc.GetString("bank-radio-fine-revoked",
            ("amount", record.Amount),
            ("target", record.TargetName),
            ("manager", managerName)));
        _adminLog.Add(LogType.Action, LogImpact.Medium,
            $"{managerName} revoked fine {record.Id} of {record.Amount} credits for {record.TargetName} issued by {record.IssuerName}");
        message = "bank-manage-fine-revoked";
        return true;
    }

    /// <summary>
    /// Возвращает данные для интерфейса о запросах на подтверждение на станции.
    /// </summary>
    public List<BankManageRecordUiData> GetRequests(Entity<StationBankManagementComponent?> station)
    {
        if (!Resolve(station, ref station.Comp, false))
            return new List<BankManageRecordUiData>();

        return station.Comp.Requests.Select(GetUiData).ToList();
    }

    /// <summary>
    /// Возвращает данные для интерфейса о последних штрафах на станции, от новых к старым.
    /// </summary>
    public List<BankManageRecordUiData> GetFines(Entity<StationBankManagementComponent?> station)
    {
        if (!Resolve(station, ref station.Comp, false))
            return new List<BankManageRecordUiData>();

        return station.Comp.Fines.AsEnumerable().Reverse().Select(GetUiData).ToList();
    }

    private void PayBonus(Entity<StationBankManagementComponent> management, Entity<StationBankAccountComponent> station, EntityUid? source, BankManageRecord record, string? manager)
    {
        _cargo.UpdateBankAccount(station.AsNullable(), -record.Amount, record.Budget);
        _bank.Deposit(record.Target, record.Amount, management.Comp.BonusReason, record.IssuerName, record.Comment);

        Announce(source, GetBudgetChannel(record.Budget), Loc.GetString(manager == null ? "bank-radio-bonus" : "bank-radio-bonus-approved",
            ("amount", record.Amount),
            ("target", record.TargetName),
            ("issuer", record.IssuerName),
            ("manager", manager ?? string.Empty),
            ("comment", record.Comment)));
        _adminLog.Add(LogType.Action, LogImpact.Medium,
            $"{record.IssuerName} paid a bonus of {record.Amount} credits from the {record.Budget} budget to {record.TargetName}{(manager == null ? string.Empty : $", approved by {manager}")}: {record.Comment}");
    }

    private void ApplyFine(Entity<StationBankManagementComponent> management, Entity<StationBankAccountComponent> station, EntityUid? source, BankManageRecord record, string? manager)
    {
        _bank.Charge(record.Target, record.Amount, management.Comp.FineReason, record.IssuerName, record.Comment);
        _cargo.UpdateBankAccount(station.AsNullable(), record.Amount, record.Budget);

        if (record.Id == 0)
            record.Id = management.Comp.NextId++;

        management.Comp.Fines.Add(record);
        if (management.Comp.Fines.Count > management.Comp.MaxFines)
            management.Comp.Fines.RemoveRange(0, management.Comp.Fines.Count - management.Comp.MaxFines);

        Announce(source, GetBudgetChannel(record.Budget), Loc.GetString(manager == null ? "bank-radio-fine" : "bank-radio-fine-approved",
            ("amount", record.Amount),
            ("target", record.TargetName),
            ("issuer", record.IssuerName),
            ("manager", manager ?? string.Empty),
            ("comment", record.Comment)));
        _adminLog.Add(LogType.Action, LogImpact.Medium,
            $"{record.IssuerName} fined {record.TargetName} for {record.Amount} credits into the {record.Budget} budget{(manager == null ? string.Empty : $", approved by {manager}")}: {record.Comment}");
    }

    private void RequestApproval(Entity<StationBankManagementComponent> management, EntityUid source, BankManageRecord record)
    {
        record.Id = management.Comp.NextId++;
        management.Comp.Requests.Add(record);

        Announce(source, management.Comp.RequestChannel, Loc.GetString(record.Fine ? "bank-radio-request-fine" : "bank-radio-request-bonus",
            ("amount", record.Amount),
            ("target", record.TargetName),
            ("issuer", record.IssuerName),
            ("comment", record.Comment)));
        _adminLog.Add(LogType.Action, LogImpact.Medium,
            $"{record.IssuerName} requested approval for a {(record.Fine ? "fine" : "bonus")} of {record.Amount} credits for {record.TargetName}: {record.Comment}");
    }

    private void PlaySound(Entity<BankCartridgeComponent> ent, SoundSpecifier? sound, EntityUid recipient, EntityUid source)
    {
        if (ent.Comp.LastSoundTime == _timing.CurTime)
            return;

        ent.Comp.LastSoundTime = _timing.CurTime;
        _audio.PlayEntity(sound, recipient, source);
    }

    private void Succeed(Entity<BankCartridgeComponent> ent, string message, EntityUid loader, EntityUid user)
    {
        _popup.PopupEntity(message, loader, user);
        PlaySound(ent, ent.Comp.SuccessSound, user, loader);
    }

    private void Refuse(Entity<BankCartridgeComponent> ent, string message, EntityUid loader, EntityUid user)
    {
        _popup.PopupEntity(message, loader, user);
        PlaySound(ent, ent.Comp.DenySound, user, loader);
    }

    private static BankManageRecord CreateRecord(
        bool fine,
        Entity<BankAccountComponent> issuer,
        Entity<BankAccountComponent> target,
        ProtoId<CargoAccountPrototype> budget,
        int amount,
        string comment)
    {
        return new BankManageRecord
        {
            Fine = fine,
            Issuer = issuer.Owner,
            Target = target.Owner,
            Budget = budget,
            Amount = amount,
            Comment = comment,
            IssuerName = issuer.Comp.OwnerName,
            TargetName = target.Comp.OwnerName,
        };
    }

    private void Announce(EntityUid? source, ProtoId<RadioChannelPrototype> channel, string message)
    {
        if (source is { } uid)
            _radio.SendRadioMessage(uid, message, channel, uid);
    }

    private ProtoId<RadioChannelPrototype> GetBudgetChannel(ProtoId<CargoAccountPrototype> budget)
    {
        return _proto.Index(budget).RadioChannel;
    }

    private void Notify(EntityUid account, string message)
    {
        var query = EntityQueryEnumerator<BankCardComponent>();
        while (query.MoveNext(out var uid, out var card))
        {
            if (card.Account != account || !TryGetCardLoader((uid, card), out var pda, out var loader))
                continue;

            _cartridgeLoader.SendNotification(pda.Value, Loc.GetString("bank-program-name"), message, loader);
        }
    }

    private bool TryGetCardLoader(
        Entity<BankCardComponent> card,
        [NotNullWhen(true)] out EntityUid? pda,
        [NotNullWhen(true)] out CartridgeLoaderComponent? loader)
    {
        pda = null;
        loader = null;
        if (!_container.TryGetContainingContainer((card, null, null), out var container)
            || !TryComp(container.Owner, out loader))
            return false;

        pda = container.Owner;
        return true;
    }

    private bool TryGetManageContext(
        Entity<BankCartridgeComponent> ent,
        EntityUid loader,
        EntityUid user,
        int number,
        int amount,
        string rawComment,
        [NotNullWhen(true)] out (Entity<BankAccountComponent> Issuer, Entity<BankAccountComponent> Target, BankAuthority Authority, Entity<StationBankAccountComponent> Station, Entity<StationBankManagementComponent> Management, string Comment)? context)
    {
        context = null;

        if (!TryGetLoaderAccount(loader, out var card, out var issuer)
            || GetAuthority(ent.Comp, card.Value) is not { } authority)
        {
            Refuse(ent, Loc.GetString("bank-manage-denied"), loader, user);
            return false;
        }

        if (amount <= 0)
        {
            Refuse(ent, Loc.GetString("bank-transfer-invalid-amount"), loader, user);
            return false;
        }

        if (!_bank.TryFindAccount(number, out var target) || target.Value.Owner == issuer.Value.Owner)
        {
            Refuse(ent, Loc.GetString("bank-transfer-invalid-account"), loader, user);
            return false;
        }

        var comment = rawComment.Trim();
        if (comment.Length == 0)
        {
            Refuse(ent, Loc.GetString("bank-manage-no-comment"), loader, user);
            return false;
        }

        if (comment.Length > ent.Comp.MaxCommentLength)
            comment = comment[..ent.Comp.MaxCommentLength];

        if (_station.GetOwningStation(loader) is not { } station
            || !TryComp<StationBankAccountComponent>(station, out var stationBank)
            || !TryComp<StationBankManagementComponent>(station, out var management))
        {
            Refuse(ent, Loc.GetString("bank-manage-no-station"), loader, user);
            return false;
        }

        context = (issuer.Value, target.Value, authority, (station, stationBank), (station, management), comment);
        return true;
    }

    private bool TryGetManager(
        Entity<BankCartridgeComponent> ent,
        EntityUid loader,
        EntityUid user,
        Func<BankAuthority, bool> permission,
        [NotNullWhen(true)] out Entity<BankAccountComponent>? manager,
        out EntityUid station)
    {
        station = default;

        if (!TryGetLoaderAccount(loader, out var card, out manager)
            || GetAuthority(ent.Comp, card.Value) is not { } authority
            || !permission(authority))
        {
            Refuse(ent, Loc.GetString("bank-manage-denied"), loader, user);
            return false;
        }

        if (_station.GetOwningStation(loader) is not { } stationUid
            || !HasComp<StationBankManagementComponent>(stationUid))
        {
            Refuse(ent, Loc.GetString("bank-manage-no-station"), loader, user);
            return false;
        }

        station = stationUid;
        return true;
    }

    private void UpdateAllUiStates()
    {
        var query = EntityQueryEnumerator<BankCartridgeComponent, CartridgeComponent>();
        while (query.MoveNext(out var uid, out var bank, out var cartridge))
        {
            if (cartridge.LoaderUid is not { } loader
                || !TryComp<CartridgeLoaderComponent>(loader, out var loaderComp)
                || loaderComp.ActiveProgram != uid)
                continue;

            UpdateUiState((uid, bank), loader);
        }
    }

    private void UpdateUiState(Entity<BankCartridgeComponent> ent, EntityUid loader)
    {
        if (!_economyZone.IsEconomyEnabled())
        {
            _cartridgeLoader.UpdateCartridgeUiState(loader, new BankUiState(null, null, null, 0, new List<BankTransaction>(), offline: true));
            return;
        }

        if (!TryGetLoaderAccount(loader, out var card, out var account))
        {
            var canOpen = TryComp<PdaComponent>(loader, out var pda) && pda.ContainedId is { } id && _bank.CanOpenAccount(id);
            _cartridgeLoader.UpdateCartridgeUiState(loader, new BankUiState(null, null, null, 0, new List<BankTransaction>(), canOpen: canOpen));
            return;
        }

        var comp = account.Value.Comp;
        var jobTitle = comp.Job is { } job && _proto.Resolve(job, out var jobProto) ? jobProto.LocalizedName : null;
        var state = new BankUiState(comp.Number,
            comp.OwnerName,
            jobTitle,
            comp.Balance,
            new List<BankTransaction>(comp.History),
            GetManageState(ent.Comp, loader, card.Value, account.Value));
        _cartridgeLoader.UpdateCartridgeUiState(loader, state);
    }

    private BankManageUiState? GetManageState(BankCartridgeComponent comp, EntityUid loader, EntityUid card, Entity<BankAccountComponent> account)
    {
        if (GetAuthority(comp, card) is not { } authority || !_proto.Resolve(authority.Budget, out var budget))
            return null;

        var station = _station.GetOwningStation(loader);
        var budgetBalance = TryComp<StationBankAccountComponent>(station, out var stationBank)
            ? stationBank.Accounts.GetValueOrDefault(authority.Budget)
            : 0;

        var requests = new List<BankManageRecordUiData>();
        var fines = new List<BankManageRecordUiData>();
        if (TryComp<StationBankManagementComponent>(station, out var management))
        {
            if (IsApprover(comp, loader))
                requests.AddRange(management.Requests.Select(GetUiData));

            if (authority.RevokeFines)
                fines.AddRange(management.Fines.AsEnumerable().Reverse().Select(GetUiData));
        }

        return new BankManageUiState(Loc.GetString(budget.Name),
            budgetBalance,
            Math.Max(0, authority.BonusLimit - account.Comp.BonusesIssued),
            authority.UnlimitedFine ? null : authority.MaxFine,
            comp.MaxCommentLength,
            requests,
            fines);
    }

    private bool IsApprover(BankCartridgeComponent comp, EntityUid loader)
    {
        return TryGetLoaderAccount(loader, out var card, out _)
               && CompOrNull<IdCardComponent>(card)?.JobPrototype is { } job
               && GetApproverJob(comp) == job;
    }

    private ProtoId<JobPrototype>? GetApproverJob(BankCartridgeComponent comp)
    {
        var present = new HashSet<ProtoId<JobPrototype>>();
        var query = EntityQueryEnumerator<ActorComponent, MobStateComponent>();
        while (query.MoveNext(out var uid, out _, out var mobState))
        {
            if (!_mobState.IsAlive(uid, mobState)
                || !_idCard.TryFindIdCard(uid, out var idCard)
                || idCard.Comp.JobPrototype is not { } job)
                continue;

            present.Add(job);
        }

        foreach (var job in comp.Approvers)
        {
            if (present.Contains(job))
                return job;
        }

        return null;
    }

    private BankManageRecordUiData GetUiData(BankManageRecord record)
    {
        var budget = _proto.Resolve(record.Budget, out var budgetProto) ? Loc.GetString(budgetProto.Name) : record.Budget.Id;
        return new BankManageRecordUiData(record.Id,
            record.Fine,
            record.IssuerName,
            record.TargetName,
            budget,
            record.Amount,
            record.Comment);
    }

    private BankAuthority? GetAuthority(BankCartridgeComponent comp, EntityUid card)
    {
        var tags = _access.TryGetTags(card) is { } cardTags
            ? new HashSet<ProtoId<AccessLevelPrototype>>(cardTags)
            : new HashSet<ProtoId<AccessLevelPrototype>>();
        var job = CompOrNull<IdCardComponent>(card)?.JobPrototype;

        foreach (var authority in comp.Authorities)
        {
            if (authority.Access is { } access && !tags.Contains(access))
                continue;

            if (authority.Job is { } authorityJob && authorityJob != job)
                continue;

            return authority;
        }

        return null;
    }

    private bool TryGetLoaderAccount(
        EntityUid loader,
        [NotNullWhen(true)] out EntityUid? card,
        [NotNullWhen(true)] out Entity<BankAccountComponent>? account)
    {
        card = null;
        account = null;
        if (!TryComp<PdaComponent>(loader, out var pda) || pda.ContainedId is not { } id)
            return false;

        card = id;
        return _bank.TryGetAccount(id, out account);
    }
}
