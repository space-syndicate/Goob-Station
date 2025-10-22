using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Imperial.Lavaland.MiningWeapons.Events;

[Serializable, NetSerializable]
public sealed class ShieldActivatedEvent : EntityEventArgs
{
    public NetEntity Smasher { get; }
    public NetEntity User { get; }
    public SpriteSpecifier? Effect { get; }

    public ShieldActivatedEvent(NetEntity smasher, NetEntity user, SpriteSpecifier? effect)
    {
        Smasher = smasher;
        User = user;
        Effect = effect;
    }
}
