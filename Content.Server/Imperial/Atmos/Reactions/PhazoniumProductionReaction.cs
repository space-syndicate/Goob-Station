using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Reactions;
using JetBrains.Annotations;
using Content.Server.Atmos.Reactions;
using Content.Server.Atmos;

namespace Content.Server.Imperial.Atmos.Reactions;

[UsedImplicitly]
public sealed partial class PhazoniumProductionReaction : IGasReactionEffect
{
    public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem, float heatScale)
    {
        var initialTherm = mixture.GetMoles(Gas.Thermonium);
        var initialOzon = mixture.GetMoles(Gas.Ozonium);

        var efficiency = mixture.Temperature / Atmospherics.PhazoniumProductionMaxEfficiencyTemperature;
        var loss = 1 - efficiency;

        var thrmConversion = initialTherm / Atmospherics.PhazoniumProductionConversionRate;
        var ozonConversion = initialOzon / Atmospherics.PhazoniumProductionConversionRate;
        var total = thrmConversion + ozonConversion;

        mixture.AdjustMoles(Gas.Thermonium, -thrmConversion);
        mixture.AdjustMoles(Gas.Ozonium, -ozonConversion);
        mixture.AdjustMoles(Gas.Phazonium, total * efficiency);
        mixture.AdjustMoles(Gas.Frezon, total * loss);

        return ReactionResult.Reacting;
    }

}
