// Возможно Подлежит удалению
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Reactions;
using JetBrains.Annotations;
using Content.Server.Atmos.Reactions;
using Content.Server.Atmos;

namespace Content.Server.Imperial.Atmos.Reactions;

[UsedImplicitly]
public sealed partial class DeuteriumProductionReaction : IGasReactionEffect
{
    public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem, float heatScale)
    {
        var initialAntiNoblium = mixture.GetMoles(Gas.AntiNoblium);
        if (initialAntiNoblium > 5f)
            return ReactionResult.NoReaction;
        var initialHydrogen = mixture.GetMoles(Gas.Hydrogen);
        var initialNitr = mixture.GetMoles(Gas.Nitrogen);

        var efficiency = mixture.Temperature / Atmospherics.DeuteriumProductionMaxEfficiencyTemperature;

        var hydrConversion = initialHydrogen / Atmospherics.DeuteriumProductionConversionRate;
        var nitrConversion = initialNitr / Atmospherics.DeuteriumProductionConversionRate;
        var total = nitrConversion + hydrConversion;

        mixture.AdjustMoles(Gas.Hydrogen, -hydrConversion);
        mixture.AdjustMoles(Gas.Nitrogen, -nitrConversion);
        mixture.AdjustMoles(Gas.Deuterium, total * efficiency);
        mixture.AdjustMoles(Gas.Tritium, total * efficiency);

        return ReactionResult.Reacting;
    }

}
