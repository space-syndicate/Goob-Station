using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Content.Shared.Imperial.Atmospheric.RCD;

namespace Content.Shared.Imperial.Atmospheric.RCD;

[Serializable, NetSerializable]
public sealed class AtmosphericRCDSystemMessage(ProtoId<AtmosphericRCDPrototype> protoId) : BoundUserInterfaceMessage
{
    public ProtoId<AtmosphericRCDPrototype> ProtoId = protoId;
}
