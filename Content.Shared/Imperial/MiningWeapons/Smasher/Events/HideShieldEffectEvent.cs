using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Imperial.MiningWeapons.Smasher.Events;

[Serializable, NetSerializable]
public sealed class HideShieldEffectEvent : EntityEventArgs
{
    public NetEntity Uid;

    public HideShieldEffectEvent(NetEntity uid)
    {
        Uid = uid;
    }
}
