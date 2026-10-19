using System.Diagnostics.CodeAnalysis;
using Content.Server._Aquila.Economy.Components;
using Content.Server.GameTicking;
using Content.Shared._Aquila.CCVars;
using Content.Shared._Aquila.Economy.DataDefinitions;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Examine;
using Content.Shared.GameTicking;
using Content.Shared.Roles;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Aquila.Economy.Systems;

public sealed class BankAccountSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly SharedIdCardSystem _idCard = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
        SubscribeLocalEvent<BankCardComponent, ExaminedEvent>(OnExamined);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var enabled = _cfg.GetCVar(AquilaCVars.Economy);
        var query = EntityQueryEnumerator<BankAccountComponent>();
        while (query.MoveNext(out var uid, out var account))
        {
            if (_timing.CurTime < account.NextPayday)
                continue;

            account.NextPayday += account.PaydayInterval;
            if (enabled)
                Deposit((uid, account), account.Salary, account.SalaryReason);
        }
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        if (!_cfg.GetCVar(AquilaCVars.Economy)
            || !_idCard.TryFindIdCard(args.Mob, out var idCard)
            || !TryComp<BankCardComponent>(idCard, out var card)
            || card.Account != null)
            return;

        ProtoId<JobPrototype>? job = args.JobId is { } jobId ? new ProtoId<JobPrototype>(jobId) : (ProtoId<JobPrototype>?) null;
        var account = CreateAccount((idCard.Owner, card), args.Profile.Name, job);
        account.Comp.Salary = job is { } salaryJob
            ? account.Comp.Salaries.GetValueOrDefault(salaryJob, account.Comp.DefaultSalary)
            : account.Comp.DefaultSalary;

        Deposit(account.AsNullable(), account.Comp.Salary * account.Comp.StartingPaychecks, account.Comp.StartingReason);
    }

    /// <summary>
    /// Проверяет, можно ли оформить на ID-карту новый банковский счёт: у карты его ещё нет.
    /// </summary>
    public bool CanOpenAccount(EntityUid idCard)
    {
        return _cfg.GetCVar(AquilaCVars.Economy)
               && HasComp<IdCardComponent>(idCard)
               && (!TryComp<BankCardComponent>(idCard, out var card) || card.Account == null);
    }

    /// <summary>
    /// Оформляет на ID-карту новый банковский счёт на имя, указанное на карте, а если его нет, на запасное имя. Такой счёт открывается пустым и без зарплаты.
    /// </summary>
    public bool TryOpenAccount(EntityUid idCard, string fallbackName, [NotNullWhen(true)] out Entity<BankAccountComponent>? account)
    {
        account = null;
        if (!CanOpenAccount(idCard))
            return false;

        var card = EnsureComp<BankCardComponent>(idCard);
        var id = Comp<IdCardComponent>(idCard);
        var ownerName = string.IsNullOrWhiteSpace(id.FullName) ? fallbackName : id.FullName;
        account = CreateAccount((idCard, card), ownerName, id.JobPrototype);
        return true;
    }

    private Entity<BankAccountComponent> CreateAccount(Entity<BankCardComponent> card, string ownerName, ProtoId<JobPrototype>? job)
    {
        var uid = Spawn(card.Comp.AccountPrototype);
        var account = Comp<BankAccountComponent>(uid);

        account.Number = GenerateNumber(account);
        account.OwnerName = ownerName;
        account.Job = job;
        account.NextPayday = _timing.CurTime + account.PaydayInterval;
        card.Comp.Account = uid;
        return (uid, account);
    }

    private void OnExamined(Entity<BankCardComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange || !TryGetAccount(ent.AsNullable(), out var account))
            return;

        args.PushMarkup(Loc.GetString("bank-card-examine-number", ("number", account.Value.Comp.Number.ToString())));
    }

    /// <summary>
    /// Ищет счёт, привязанный к карте.
    /// </summary>
    public bool TryGetAccount(Entity<BankCardComponent?> card, [NotNullWhen(true)] out Entity<BankAccountComponent>? account)
    {
        account = null;
        if (!Resolve(card, ref card.Comp, false)
            || card.Comp.Account is not { } uid
            || !TryComp<BankAccountComponent>(uid, out var comp))
            return false;

        account = (uid, comp);
        return true;
    }

    /// <summary>
    /// Ищет счёт по его номеру.
    /// </summary>
    public bool TryFindAccount(int number, [NotNullWhen(true)] out Entity<BankAccountComponent>? account)
    {
        var query = EntityQueryEnumerator<BankAccountComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Number != number)
                continue;

            account = (uid, comp);
            return true;
        }

        account = null;
        return false;
    }

    /// <summary>
    /// Зачисляет деньги на счёт.
    /// </summary>
    public void Deposit(Entity<BankAccountComponent?> account, int amount, LocId reason, string? party = null, string? comment = null)
    {
        if (!Resolve(account, ref account.Comp) || amount <= 0)
            return;

        account.Comp.Balance += amount;
        AddTransaction((account, account.Comp), amount, reason, party, comment);
    }

    /// <summary>
    /// Списывает деньги со счёта, если на нём хватает средств.
    /// </summary>
    public bool TryWithdraw(Entity<BankAccountComponent?> account, int amount, LocId reason, string? party = null)
    {
        if (!Resolve(account, ref account.Comp) || amount <= 0 || account.Comp.Balance < amount)
            return false;

        account.Comp.Balance -= amount;
        AddTransaction((account, account.Comp), -amount, reason, party);
        return true;
    }

    /// <summary>
    /// Списывает деньги со счёта, даже если баланс уйдёт в минус. Долг гасится следующими зачислениями.
    /// </summary>
    public void Charge(Entity<BankAccountComponent?> account, int amount, LocId reason, string? party = null, string? comment = null)
    {
        if (!Resolve(account, ref account.Comp) || amount <= 0)
            return;

        account.Comp.Balance -= amount;
        AddTransaction((account, account.Comp), -amount, reason, party, comment);
    }

    /// <summary>
    /// Учитывает выписанную владельцем счёта премию, если она укладывается в лимит на смену.
    /// </summary>
    public bool TryReserveBonus(Entity<BankAccountComponent?> issuer, int amount, int limit)
    {
        if (!Resolve(issuer, ref issuer.Comp) || amount <= 0 || issuer.Comp.BonusesIssued + amount > limit)
            return false;

        issuer.Comp.BonusesIssued += amount;
        return true;
    }

    /// <summary>
    /// Переводит деньги с одного счёта на другой, если на счёте отправителя хватает средств.
    /// </summary>
    public bool TryTransfer(Entity<BankAccountComponent?> from, Entity<BankAccountComponent?> to, int amount)
    {
        if (!Resolve(from, ref from.Comp)
            || !Resolve(to, ref to.Comp)
            || from.Owner == to.Owner
            || !TryWithdraw(from, amount, from.Comp.TransferOutReason, to.Comp.OwnerName))
            return false;

        Deposit(to, amount, to.Comp.TransferInReason, from.Comp.OwnerName);
        return true;
    }

    private void AddTransaction(Entity<BankAccountComponent> account, int amount, LocId reason, string? party, string? comment = null)
    {
        var transaction = new BankTransaction
        {
            Time = _gameTicker.RoundDuration(),
            Amount = amount,
            Reason = reason,
            Party = party,
            Comment = comment,
        };

        account.Comp.History.Add(transaction);
        if (account.Comp.History.Count > account.Comp.MaxHistory)
            account.Comp.History.RemoveRange(0, account.Comp.History.Count - account.Comp.MaxHistory);

        var ev = new BankTransactionEvent(account, transaction);
        var query = EntityQueryEnumerator<BankCardComponent>();
        while (query.MoveNext(out var uid, out var card))
        {
            if (card.Account == account.Owner)
                RaiseLocalEvent(uid, ref ev);
        }
    }

    private int GenerateNumber(BankAccountComponent account)
    {
        int number;
        do
        {
            number = _random.Next(account.MinNumber, account.MaxNumber);
        }
        while (TryFindAccount(number, out _));

        return number;
    }
}
