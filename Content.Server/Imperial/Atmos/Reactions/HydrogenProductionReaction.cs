using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Reactions;
using JetBrains.Annotations;
using Content.Server.Atmos.Reactions;
using Content.Server.Atmos;

namespace Content.Server.Imperial.Atmos.Reactions;

[UsedImplicitly]
public sealed partial class HydrogenProductionReaction : IGasReactionEffect
{
    public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem, float heatScale)
    {
        var initialOxy = mixture.GetMoles(Gas.Oxygen);
        var initialVapor = mixture.GetMoles(Gas.WaterVapor);

        var efficiency = mixture.Temperature / Atmospherics.HydrogenProductionMaxEfficiencyTemperature;

        var oxyConversion = initialOxy / Atmospherics.HydrogenProductionConversionRate;
        var vaporConversion = initialVapor / Atmospherics.HydrogenProductionConversionRate;
        var total = oxyConversion + vaporConversion;

        mixture.AdjustMoles(Gas.Oxygen, -oxyConversion);
        mixture.AdjustMoles(Gas.WaterVapor, -vaporConversion);
        mixture.AdjustMoles(Gas.Hydrogen, total * efficiency);
        mixture.AdjustMoles(Gas.Ozonium, total * efficiency);

        return ReactionResult.Reacting;
    }

}
