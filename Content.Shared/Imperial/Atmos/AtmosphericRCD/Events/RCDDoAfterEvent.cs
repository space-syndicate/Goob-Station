using Content.Shared.DoAfter;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.Atmospheric.RCD.Systems;

[Serializable, NetSerializable]
public sealed partial class RCDDoAfterEvent : DoAfterEvent
{
    [DataField(required: true)]
    public NetCoordinates Location { get; private set; }

    [DataField]
    public Direction Direction { get; private set; }

    [DataField]
    public ProtoId<AtmosphericRCDPrototype> StartingProtoId { get; private set; }

    [DataField]
    public int Cost { get; private set; } = 1;

    [DataField("fx")]
    public NetEntity? Effect { get; private set; }

    private RCDDoAfterEvent() { }

    public RCDDoAfterEvent(NetCoordinates location, Direction direction, ProtoId<AtmosphericRCDPrototype> startingProtoId, int cost, NetEntity? effect = null)
    {
        Location = location;
        Direction = direction;
        StartingProtoId = startingProtoId;
        Cost = cost;
        Effect = effect;
    }

    public override DoAfterEvent Clone()
    {
        return this;
    }
}
