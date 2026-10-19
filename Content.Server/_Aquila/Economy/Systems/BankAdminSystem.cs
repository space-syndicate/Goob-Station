using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server._Aquila.Economy.Components;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.EUI;
using Content.Server.Station.Systems;
using Content.Shared._Aquila.Economy;
using Content.Shared._Aquila.Economy.DataDefinitions;
using Content.Shared._Aquila.Economy.Systems;
using Content.Shared.Access.Systems;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.Verbs;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Aquila.Economy.Systems;

public sealed class BankAdminSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IAdminManager _admin = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly EuiManager _eui = default!;
    [Dependency] private readonly BankAccountSystem _bank = default!;
    [Dependency] private readonly BankCartridgeSystem _bankCartridge = default!;
    [Dependency] private readonly EconomyZoneSystem _economyZone = default!;
    [Dependency] private readonly SharedIdCardSystem _idCard = default!;
    [Dependency] private readonly StationSystem _station = default!;

    private static readonly LocId CentralCommandParty = "bank-party-central-command";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GetVerbsEvent<Verb>>(OnGetVerbs);
    }

    private void OnGetVerbs(GetVerbsEvent<Verb> args)
    {
        if (!TryComp<ActorComponent>(args.User, out var actor) || !CanManage(actor.PlayerSession))
            return;

        var xform = Transform(args.Target);
        AddZoneVerb(args, actor.PlayerSession, xform.GridUid, "bank-admin-verb-zone-grid-off", "bank-admin-verb-zone-grid-on");
        AddZoneVerb(args, actor.PlayerSession, xform.MapUid, "bank-admin-verb-zone-map-off", "bank-admin-verb-zone-map-on");

        if (!TryGetTargetAccount(args.Target, out var account))
            return;

        var session = actor.PlayerSession;
        var number = account.Value.Comp.Number;
        args.Verbs.Add(new Verb
        {
            Text = Loc.GetString("bank-admin-verb-panel"),
            Category = VerbCategory.Admin,
            Impact = LogImpact.Low,
            Act = () => OpenPanel(session, number),
        });
    }

    private void AddZoneVerb(GetVerbsEvent<Verb> args, ICommonSession admin, EntityUid? zone, LocId disableText, LocId enableText)
    {
        if (zone is not { } uid)
            return;

        var free = _economyZone.IsFreeZone(uid);
        args.Verbs.Add(new Verb
        {
            Text = Loc.GetString(free ? enableText : disableText),
            Category = VerbCategory.Admin,
            Impact = LogImpact.High,
            Act = () =>
            {
                _economyZone.SetFreeZone(uid, !free);
                _adminLog.Add(LogType.Action, LogImpact.High,
                    $"{admin.Name} {(free ? "enabled" : "disabled")} the economy on {ToPrettyString(uid)}");
            },
        });
    }

    /// <summary>
    /// Проверяет, может ли игрок управлять счетами от лица ЦК.
    /// </summary>
    public bool CanManage(ICommonSession session)
    {
        return _admin.HasAdminFlag(session, AdminFlags.Admin);
    }

    /// <summary>
    /// Открывает админу панель "Банк ЦК", сразу на нужном счёте, если он указан.
    /// </summary>
    public void OpenPanel(ICommonSession admin, int? number = null)
    {
        if (!CanManage(admin))
            return;

        _eui.OpenEui(new BankAdminEui(number), admin);
    }

    /// <summary>
    /// Собирает состояние панели "Банк ЦК".
    /// </summary>
    public BankAdminEuiState GetPanelState(int? selected)
    {
        var accounts = new List<BankAdminAccountData>();
        var query = EntityQueryEnumerator<BankAccountComponent>();
        while (query.MoveNext(out var account))
        {
            var jobTitle = account.Job is { } job && _proto.Resolve(job, out var jobProto) ? jobProto.LocalizedName : null;
            accounts.Add(new BankAdminAccountData(account.Number, account.OwnerName, jobTitle, account.Balance));
        }

        accounts.Sort((a, b) => string.Compare(a.OwnerName, b.OwnerName, StringComparison.CurrentCultureIgnoreCase));

        var history = selected is { } number && _bank.TryFindAccount(number, out var selectedAccount)
            ? new List<BankTransaction>(selectedAccount.Value.Comp.History)
            : new List<BankTransaction>();

        var station = GetManagedStation();
        return new BankAdminEuiState(accounts,
            selected,
            history,
            station is { } requestStation ? _bankCartridge.GetRequests(requestStation) : new List<BankManageRecordUiData>(),
            station is { } fineStation ? _bankCartridge.GetFines(fineStation) : new List<BankManageRecordUiData>());
    }

    /// <summary>
    /// Начисляет премию от ЦК на счёт.
    /// </summary>
    public void Bonus(ICommonSession admin, int number, int amount, string comment)
    {
        if (amount <= 0 || !TryGetOperation(admin, number, comment, out var account, out var text))
            return;

        _bank.Deposit(account.Value.AsNullable(), amount, account.Value.Comp.AdminBonusReason, GetParty(account.Value), text);
        _adminLog.Add(LogType.Action, LogImpact.High,
            $"{admin.Name} paid a Central Command bonus of {amount} credits to account {number} ({account.Value.Comp.OwnerName}): {text}");
    }

    /// <summary>
    /// Списывает со счёта штраф от ЦК.
    /// </summary>
    public void Fine(ICommonSession admin, int number, int amount, string comment)
    {
        if (amount <= 0 || !TryGetOperation(admin, number, comment, out var account, out var text))
            return;

        _bank.Charge(account.Value.AsNullable(), amount, account.Value.Comp.AdminFineReason, GetParty(account.Value), text);
        _adminLog.Add(LogType.Action, LogImpact.High,
            $"{admin.Name} issued a Central Command fine of {amount} credits to account {number} ({account.Value.Comp.OwnerName}): {text}");
    }

    /// <summary>
    /// Доводит баланс счёта до нужной суммы одной операцией от ЦК.
    /// </summary>
    public void SetBalance(ICommonSession admin, int number, int balance, string comment)
    {
        if (!TryGetOperation(admin, number, comment, out var account, out var text))
            return;

        var difference = balance - account.Value.Comp.Balance;
        if (difference > 0)
            _bank.Deposit(account.Value.AsNullable(), difference, account.Value.Comp.AdjustReason, GetParty(account.Value), text);
        else if (difference < 0)
            _bank.Charge(account.Value.AsNullable(), -difference, account.Value.Comp.AdjustReason, GetParty(account.Value), text);

        _adminLog.Add(LogType.Action, LogImpact.High,
            $"{admin.Name} set the balance of account {number} ({account.Value.Comp.OwnerName}) to {balance} credits: {text}");
    }

    /// <summary>
    /// Отменяет операцию из истории счёта обратной операцией от ЦК.
    /// </summary>
    public void Revert(ICommonSession admin, int number, int index, int amount, string comment)
    {
        if (!TryGetOperation(admin, number, comment, out var account, out var text))
            return;

        var history = account.Value.Comp.History;
        if (index < 0 || index >= history.Count || history[index].Amount != amount)
            return;

        var transaction = history[index];
        if (transaction.Amount > 0)
            _bank.Charge(account.Value.AsNullable(), transaction.Amount, account.Value.Comp.RevertReason, GetParty(account.Value), text);
        else
            _bank.Deposit(account.Value.AsNullable(), -transaction.Amount, account.Value.Comp.RevertReason, GetParty(account.Value), text);

        _adminLog.Add(LogType.Action, LogImpact.High,
            $"{admin.Name} reverted a transaction of {transaction.Amount} credits ({transaction.Reason}) on account {number} ({account.Value.Comp.OwnerName}): {text}");
    }

    /// <summary>
    /// Подтверждает или отклоняет запрос на премию или штраф от лица ЦК.
    /// </summary>
    public void ResolveRequest(ICommonSession admin, int id, bool approve)
    {
        if (!CanManage(admin) || GetManagedStation() is not { } station)
            return;

        if (_bankCartridge.TryResolveRequest(station, id, approve, Loc.GetString(CentralCommandParty), _station.GetLargestGrid(station), out _))
            _adminLog.Add(LogType.Action, LogImpact.High, $"{admin.Name} {(approve ? "approved" : "rejected")} bank request {id} as Central Command");
    }

    /// <summary>
    /// Отменяет штраф от лица ЦК.
    /// </summary>
    public void RevokeFine(ICommonSession admin, int id)
    {
        if (!CanManage(admin) || GetManagedStation() is not { } station)
            return;

        if (_bankCartridge.TryRevokeFine(station, id, Loc.GetString(CentralCommandParty), _station.GetLargestGrid(station), out _))
            _adminLog.Add(LogType.Action, LogImpact.High, $"{admin.Name} revoked fine {id} as Central Command");
    }

    private EntityUid? GetManagedStation()
    {
        var query = EntityQueryEnumerator<StationBankManagementComponent>();
        return query.MoveNext(out var uid, out _) ? uid : null;
    }

    private bool TryGetOperation(
        ICommonSession admin,
        int number,
        string comment,
        [NotNullWhen(true)] out Entity<BankAccountComponent>? account,
        [NotNullWhen(true)] out string? text)
    {
        account = null;
        text = comment.Trim();
        if (!CanManage(admin) || text.Length == 0 || !_bank.TryFindAccount(number, out account))
        {
            text = null;
            return false;
        }

        return true;
    }

    private string GetParty(Entity<BankAccountComponent> account)
    {
        return Loc.GetString(account.Comp.CentralCommandParty);
    }

    private bool TryGetTargetAccount(EntityUid target, [NotNullWhen(true)] out Entity<BankAccountComponent>? account)
    {
        account = null;
        if (TryComp<BankAccountComponent>(target, out var comp))
        {
            account = (target, comp);
            return true;
        }

        if (HasComp<BankCardComponent>(target))
            return _bank.TryGetAccount(target, out account);

        return _idCard.TryFindIdCard(target, out var idCard) && _bank.TryGetAccount(idCard.Owner, out account);
    }
}
