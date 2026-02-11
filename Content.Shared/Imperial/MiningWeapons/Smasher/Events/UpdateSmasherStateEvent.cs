using Content.Shared.Imperial.MiningWeapons.Smasher.Components;
using Robust.Shared.Input;
using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.MiningWeapons.Smasher.Events;

[Serializable, NetSerializable]
public sealed class UpdateSmasherStateEvent : EntityEventArgs
{
    public NetEntity User;
    public BoundKeyState KeyState;

    public UpdateSmasherStateEvent(NetEntity user, BoundKeyState keyState)
    {
        User = user;
        KeyState = keyState;
    }
}
