using Content.Shared.DoAfter;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.XenoGenetics;

[Serializable, NetSerializable]
public sealed partial class GeneInsertingDoAfterEvent : SimpleDoAfterEvent
{
}
[Serializable, NetSerializable]
public sealed partial class GeneWithdrawDoAfterEvent : SimpleDoAfterEvent
{
}
[ByRefEvent]
public readonly record struct GeneInsertedEvent(EntityUid Gene, EntityUid Target);
[ByRefEvent]
public readonly record struct GeneWithdrawnEvent(EntityUid Gene, EntityUid Target);