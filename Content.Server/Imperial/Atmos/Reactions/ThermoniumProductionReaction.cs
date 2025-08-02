using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Reactions;
using JetBrains.Annotations;
using Content.Server.Atmos.Reactions;
using Content.Server.Atmos;

namespace Content.Server.Imperial.Atmos.Reactions;

[UsedImplicitly]
public sealed partial class ThermoniumProductionReaction : IGasReactionEffect
{
    public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem, float heatScale)
    {
        var initialCarbonDioxide = mixture.GetMoles(Gas.CarbonDioxide);
        var initialNit = mixture.GetMoles(Gas.Nitrogen);
        var initialTrit = mixture.GetMoles(Gas.Tritium);

        var efficiency = mixture.Temperature / Atmospherics.ThermoniumProductionMaxEfficiencyTemperature;
        var loss = 1 - efficiency;

        var nitConversion = initialNit / Atmospherics.ThermoniumProductionConversionRate;
        var tritConversion = initialTrit / Atmospherics.ThermoniumProductionConversionRate;
        var total = nitConversion + tritConversion;

        mixture.AdjustMoles(Gas.Nitrogen, -nitConversion);
        mixture.AdjustMoles(Gas.Tritium, -tritConversion);
        mixture.AdjustMoles(Gas.Thermonium, total * efficiency);
        mixture.AdjustMoles(Gas.CarbonDioxide, total * loss);

        return ReactionResult.Reacting;
    }

}
