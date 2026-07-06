using Content.Shared.Atmos;
using Content.Shared.Imperial.Power.Components;

namespace Content.Shared.Imperial.Power.GasReactions.Reactions;

public sealed partial class SupermatterAntiNobliumShutdownReaction : ISupermatterGasReaction
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

        if (!gasComp.AntiNobliumHardShutdownEnabled)
            return;

        integrity.Activated = false;
    }
}

