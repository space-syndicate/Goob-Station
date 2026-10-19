using Content.Shared._Aquila.Economy.DataDefinitions;

namespace Content.Server._Aquila.Economy;

[ByRefEvent]
public readonly record struct BankTransactionEvent(EntityUid Account, BankTransaction Transaction);
