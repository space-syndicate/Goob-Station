using Content.Shared.Atmos;
using Content.Shared.Imperial.Power.Components;

namespace Content.Shared.Imperial.Power.GasReactions.Reactions;

public sealed partial class SupermatterEventSpeedMultiplierReaction : ISupermatterGasReaction
{
    [DataField(required: true)]
    public float Multiplier = 1f;

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

        gasComp.RuntimeEventSpeedMultiplier *= Multiplier;
    }
}

