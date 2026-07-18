using Content.Shared.Atmos;
using Content.Shared.Imperial.Power.Components;

namespace Content.Shared.Imperial.Power.GasReactions;

[ImplicitDataDefinitionForInheritors]
public partial interface ISupermatterGasReaction
{
    void React(
        EntityUid uid,
        SupermatterIntegrityComponent integrity,
        SupermatterGasComponent gasComp,
        GasMixture mixture,
        float frameTime,
        IEntityManager entMan,
        IEntitySystemManager sysMan,
        bool active);
}

