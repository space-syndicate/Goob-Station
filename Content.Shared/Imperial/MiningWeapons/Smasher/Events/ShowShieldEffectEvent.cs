using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Imperial.MiningWeapons.Smasher.Events;

[Serializable, NetSerializable]
public sealed class ShowShieldEffectEvent : EntityEventArgs
{
    /// <summary>
    /// Should the animation loop
    /// </summary>
    public bool Loop;
    public NetEntity Uid;
    public SpriteSpecifier EffectDecay;

    public ShowShieldEffectEvent(NetEntity uid, SpriteSpecifier effectDecay, bool loop)
    {
        Uid = uid;
        EffectDecay = effectDecay;
        Loop = loop;
    }
}
