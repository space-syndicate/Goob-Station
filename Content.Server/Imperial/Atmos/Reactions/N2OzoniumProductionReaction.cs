using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Reactions;
using JetBrains.Annotations;
using Content.Server.Atmos.Reactions;
using Content.Server.Atmos;

namespace Content.Server.Imperial.Atmos.Reactions;

[UsedImplicitly]
public sealed partial class N2OzoniumProductionReaction : IGasReactionEffect
{
    public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem, float heatScale)
    {
        var initialNit = mixture.GetMoles(Gas.Nitrogen);
        var initialOzon = mixture.GetMoles(Gas.Ozonium);

        var efficiency = mixture.Temperature / Atmospherics.N2OzonProductionMaxEfficiencyTemperature;

        var nitConversion = initialNit / Atmospherics.N2OzonProductionConversionRate;
        var ozonConversion = initialOzon / Atmospherics.N2OzonProductionConversionRate;
        var total = nitConversion + ozonConversion;

        mixture.AdjustMoles(Gas.Nitrogen, -nitConversion);
        mixture.AdjustMoles(Gas.Ozonium, -ozonConversion);
        mixture.AdjustMoles(Gas.NitrousOxide, total * efficiency);

        return ReactionResult.Reacting;
    }

}
