using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Imperial.Lavaland.MiningWeapons.Smasher.Events;

[Serializable, NetSerializable]
public sealed class ShowShieldEffectEvent : EntityEventArgs
{
    public NetEntity Uid;
    public SpriteSpecifier EffectDecay;

    public ShowShieldEffectEvent(NetEntity uid, SpriteSpecifier effectDecay)
    {
        Uid = uid;
        EffectDecay = effectDecay;
    }
}
