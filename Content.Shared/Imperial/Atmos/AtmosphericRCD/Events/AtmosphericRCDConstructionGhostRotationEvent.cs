using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Content.Shared.Imperial.Atmospheric.RCD;

namespace Content.Shared.Imperial.Atmospheric.RCD;

[Serializable, NetSerializable]
public sealed class AtmosphericRCDConstructionGhostRotationEvent(NetEntity netEntity, Direction direction) : EntityEventArgs
{
    public readonly NetEntity NetEntity = netEntity;
    public readonly Direction Direction = direction;
}
