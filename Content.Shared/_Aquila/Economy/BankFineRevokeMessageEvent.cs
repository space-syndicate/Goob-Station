using Content.Shared.CartridgeLoader;
using Robust.Shared.Serialization;

namespace Content.Shared._Aquila.Economy;

[Serializable, NetSerializable]
public sealed class BankFineRevokeMessageEvent(int id) : CartridgeMessageEvent
{
    public readonly int Id = id;
}
