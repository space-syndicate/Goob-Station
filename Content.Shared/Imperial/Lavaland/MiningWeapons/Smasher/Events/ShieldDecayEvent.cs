using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Imperial.Lavaland.MiningWeapons.Smasher.Events;

[Serializable, NetSerializable]
public sealed class ShieldDecayEvent : EntityEventArgs
{
    public NetEntity User { get; }
    public SpriteSpecifier? EffectDecay { get; }

    public ShieldDecayEvent(NetEntity user, SpriteSpecifier? effectDecay)
    {
        User = user;
        EffectDecay = effectDecay;
    }
}
