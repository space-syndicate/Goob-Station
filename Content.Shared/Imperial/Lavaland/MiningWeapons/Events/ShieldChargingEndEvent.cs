using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.Lavaland.MiningWeapons.Events;

[Serializable, NetSerializable]
public sealed class ShieldChargingEndEvent : EntityEventArgs
{
    public NetEntity User { get; }

    public ShieldChargingEndEvent(NetEntity user)
    {
        User = user;
    }
}
