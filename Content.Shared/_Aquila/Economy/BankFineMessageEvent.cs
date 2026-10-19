using Content.Shared.CartridgeLoader;
using Robust.Shared.Serialization;

namespace Content.Shared._Aquila.Economy;

[Serializable, NetSerializable]
public sealed class BankFineMessageEvent(int number, int amount, string comment) : CartridgeMessageEvent
{
    public readonly int Number = number;
    public readonly int Amount = amount;
    public readonly string Comment = comment;
}
