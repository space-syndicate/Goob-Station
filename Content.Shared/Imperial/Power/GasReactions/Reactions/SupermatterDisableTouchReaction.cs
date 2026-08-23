using Content.Shared.Atmos;
using Content.Shared.Imperial.Power.Components;

namespace Content.Shared.Imperial.Power.GasReactions.Reactions;

public sealed partial class SupermatterDisableTouchReaction : ISupermatterGasReaction
{
    public void React(
        EntityUid uid,
        SupermatterIntegrityComponent integrity,
        SupermatterGasComponent gasComp,
        GasMixture mixture,
        float frameTime,
        IEntityManager entMan,
        IEntitySystemManager sysMan,
        bool active)
    {
        if (!active)
            return;

        gasComp.RuntimeDisableTouchGib = true;
    }
}

