using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.Lavaland.MiningWeapons.Events;

[Serializable, NetSerializable]
public sealed class ShieldActivatedEvent : EntityEventArgs
{
    public NetEntity Smasher { get; }

    public ShieldActivatedEvent(NetEntity smasher)
    {
        Smasher = smasher;
    }
}
