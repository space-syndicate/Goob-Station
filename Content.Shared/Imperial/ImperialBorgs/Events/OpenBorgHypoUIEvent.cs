using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.ImperialBorgs.Events;

[Serializable, NetSerializable]
public sealed class OpenBorgHypoUIEvent(NetEntity entity) : EntityEventArgs
{
    public NetEntity Entity { get; } = entity;
}
