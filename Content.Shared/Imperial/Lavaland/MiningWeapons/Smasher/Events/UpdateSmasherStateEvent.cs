using Robust.Shared.Input;
using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.Lavaland.MiningWeapons.Smasher.Events;

[Serializable, NetSerializable]
public sealed class UpdateSmasherStateEvent : EntityEventArgs
{
    public NetEntity User;
    public NetEntity Smasher;
    public BoundKeyState KeyState;

    public UpdateSmasherStateEvent(NetEntity user, NetEntity smasher, BoundKeyState keyState)
    {
        User = user;
        Smasher = smasher;
        KeyState = keyState;
    }
}
