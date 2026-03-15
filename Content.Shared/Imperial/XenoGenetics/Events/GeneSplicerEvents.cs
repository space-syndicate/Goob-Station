using Robust.Shared.Prototypes;

namespace Content.Shared.Imperial.XenoGenetics;

[ByRefEvent]
public readonly record struct GeneInsertedEvent(EntityUid Gene, EntityUid Target);
[ByRefEvent]
public readonly record struct GeneWithdrawnEvent(EntityUid Gene, EntityUid Target);