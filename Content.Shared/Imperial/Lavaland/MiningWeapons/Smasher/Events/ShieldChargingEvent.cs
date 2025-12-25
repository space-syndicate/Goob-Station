using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Imperial.Lavaland.MiningWeapons.Smasher.Events;

[Serializable, NetSerializable]
public sealed class ShieldChargingEvent : EntityEventArgs
{
    public NetEntity User { get; }
    public SpriteSpecifier? EffectCharging { get; }

    public ShieldChargingEvent(NetEntity user, SpriteSpecifier? effectCharging)
    {
        User = user;
        EffectCharging = effectCharging;
    }
}
