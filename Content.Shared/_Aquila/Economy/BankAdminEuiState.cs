using Content.Shared._Aquila.Economy.DataDefinitions;
using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._Aquila.Economy;

[Serializable, NetSerializable]
public sealed class BankAdminEuiState(
    List<BankAdminAccountData> accounts,
    int? selected,
    List<BankTransaction> history,
    List<BankManageRecordUiData> requests,
    List<BankManageRecordUiData> fines)
    : EuiStateBase
{
    public readonly List<BankAdminAccountData> Accounts = accounts;
    public readonly int? Selected = selected;
    public readonly List<BankTransaction> History = history;
    public readonly List<BankManageRecordUiData> Requests = requests;
    public readonly List<BankManageRecordUiData> Fines = fines;
}

[Serializable, NetSerializable]
public sealed class BankAdminAccountData(int number, string ownerName, string? jobTitle, int balance)
{
    public readonly int Number = number;
    public readonly string OwnerName = ownerName;
    public readonly string? JobTitle = jobTitle;
    public readonly int Balance = balance;
}

[Serializable, NetSerializable]
public sealed class BankAdminSelectMessage(int number) : EuiMessageBase
{
    public readonly int Number = number;
}

[Serializable, NetSerializable]
public sealed class BankAdminRefreshMessage : EuiMessageBase;

[Serializable, NetSerializable]
public sealed class BankAdminBonusMessage(int number, int amount, string comment) : EuiMessageBase
{
    public readonly int Number = number;
    public readonly int Amount = amount;
    public readonly string Comment = comment;
}

[Serializable, NetSerializable]
public sealed class BankAdminFineMessage(int number, int amount, string comment) : EuiMessageBase
{
    public readonly int Number = number;
    public readonly int Amount = amount;
    public readonly string Comment = comment;
}

[Serializable, NetSerializable]
public sealed class BankAdminSetBalanceMessage(int number, int balance, string comment) : EuiMessageBase
{
    public readonly int Number = number;
    public readonly int Balance = balance;
    public readonly string Comment = comment;
}

[Serializable, NetSerializable]
public sealed class BankAdminRevertMessage(int number, int index, int amount, string comment) : EuiMessageBase
{
    public readonly int Number = number;
    public readonly int Index = index;
    public readonly int Amount = amount;
    public readonly string Comment = comment;
}

[Serializable, NetSerializable]
public sealed class BankAdminRequestReplyMessage(int id, bool approve) : EuiMessageBase
{
    public readonly int Id = id;
    public readonly bool Approve = approve;
}

[Serializable, NetSerializable]
public sealed class BankAdminRevokeFineMessage(int id) : EuiMessageBase
{
    public readonly int Id = id;
}
