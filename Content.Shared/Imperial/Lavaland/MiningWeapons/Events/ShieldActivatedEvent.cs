using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Imperial.Lavaland.MiningWeapons.Events;

[Serializable, NetSerializable]
public sealed class ShieldActivatedEvent : EntityEventArgs
{
    public NetEntity Smasher { get; }
    public NetEntity User { get; }
    public SpriteSpecifier? EffectActived { get; }
    public SpriteSpecifier? EffectCharging { get; }
    public SpriteSpecifier? EffectDecay { get; }

    public ShieldActivatedEvent(NetEntity smasher, NetEntity user,
        SpriteSpecifier? effectActived, SpriteSpecifier? effectCharging, SpriteSpecifier? effectDecay)
    {
        Smasher = smasher;
        User = user;
        EffectActived = effectActived;
        EffectCharging = effectCharging;
        EffectDecay = effectDecay;
    }
}
