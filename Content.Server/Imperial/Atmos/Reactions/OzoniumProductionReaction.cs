using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Reactions;
using JetBrains.Annotations;
using Content.Server.Atmos.Reactions;
using Content.Server.Atmos;

namespace Content.Server.Imperial.Atmos.Reactions;

[UsedImplicitly]
public sealed partial class OzoniumProductionReaction : IGasReactionEffect
{
    public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem, float heatScale)
    {
        var initialOxygen = mixture.GetMoles(Gas.Oxygen);

        var efficiency = mixture.Temperature / Atmospherics.OzoniumProductionMaxEfficiencyTemperature;
        var loss = 1 - efficiency;

        var oxyConversion = initialOxygen / Atmospherics.OzoniumProductionConversionRate;
        var total = oxyConversion;

        mixture.AdjustMoles(Gas.Ozonium, total * efficiency);
        mixture.AdjustMoles(Gas.Oxygen, total * loss);

        return ReactionResult.Reacting;
    }

}
