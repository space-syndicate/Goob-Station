using Content.Shared.CartridgeLoader;
using Robust.Shared.Serialization;

namespace Content.Shared._Aquila.Economy;

[Serializable, NetSerializable]
public sealed class BankTransferMessageEvent(int number, int amount) : CartridgeMessageEvent
{
    public readonly int Number = number;
    public readonly int Amount = amount;
}
