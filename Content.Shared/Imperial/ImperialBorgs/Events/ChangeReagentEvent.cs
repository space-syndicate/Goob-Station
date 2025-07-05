using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.ImperialBorgs.Events;

[Serializable, NetSerializable]
public sealed class ChangeReagentEvent(string? reagentId, NetEntity entity) : EntityEventArgs
{
    public string? ReagentId { get; } = reagentId;
    public NetEntity Entity { get; } = entity;
}
