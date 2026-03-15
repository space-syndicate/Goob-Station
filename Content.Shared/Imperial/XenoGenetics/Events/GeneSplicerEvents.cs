using Robust.Shared.Prototypes;

namespace Content.Shared.Imperial.XenoGenetics;


public readonly record struct GeneInsertedEvent(EntityUid Gene, EntityUid Target);

public readonly record struct GeneWithdrawnEvent(EntityUid Gene, EntityUid Target);