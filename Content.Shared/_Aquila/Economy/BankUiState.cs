using Content.Shared._Aquila.Economy.DataDefinitions;
using Robust.Shared.Serialization;

namespace Content.Shared._Aquila.Economy;

[Serializable, NetSerializable]
public sealed class BankUiState(
    int? number,
    string? ownerName,
    string? jobTitle,
    int balance,
    List<BankTransaction> history,
    BankManageUiState? manage = null,
    bool canOpen = false,
    bool offline = false)
    : BoundUserInterfaceState
{
    public readonly int? Number = number;
    public readonly string? OwnerName = ownerName;
    public readonly string? JobTitle = jobTitle;
    public readonly int Balance = balance;
    public readonly List<BankTransaction> History = history;
    public readonly BankManageUiState? Manage = manage;
    public readonly bool CanOpen = canOpen;
    public readonly bool Offline = offline;
}

[Serializable, NetSerializable]
public sealed class BankManageUiState(
    string budgetName,
    int budgetBalance,
    int bonusLeft,
    int? maxFine,
    int maxCommentLength,
    List<BankManageRecordUiData> requests,
    List<BankManageRecordUiData> fines)
{
    public readonly string BudgetName = budgetName;
    public readonly int BudgetBalance = budgetBalance;
    public readonly int BonusLeft = bonusLeft;
    public readonly int? MaxFine = maxFine;
    public readonly int MaxCommentLength = maxCommentLength;
    public readonly List<BankManageRecordUiData> Requests = requests;
    public readonly List<BankManageRecordUiData> Fines = fines;
}

[Serializable, NetSerializable]
public sealed class BankManageRecordUiData(int id, bool fine, string issuerName, string targetName, string budgetName, int amount, string comment)
{
    public readonly int Id = id;
    public readonly bool Fine = fine;
    public readonly string IssuerName = issuerName;
    public readonly string TargetName = targetName;
    public readonly string BudgetName = budgetName;
    public readonly int Amount = amount;
    public readonly string Comment = comment;
}
