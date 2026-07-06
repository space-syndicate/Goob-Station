using Content.Shared.Atmos;
using Content.Shared.Imperial.Power.Components;

namespace Content.Shared.Imperial.Power.GasReactions.Reactions;

public sealed partial class SupermatterIntegrityRegenReaction : ISupermatterGasReaction
{
    [DataField(required: true)]
    public float RegenPerSecond = 0.5f;

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

        integrity.Integrity = MathF.Min(integrity.MaxIntegrity, integrity.Integrity + RegenPerSecond * frameTime);
    }
}

