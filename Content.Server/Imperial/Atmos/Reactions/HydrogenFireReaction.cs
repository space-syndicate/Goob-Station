using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Reactions;
using JetBrains.Annotations;
using Content.Server.Atmos.Reactions;
using Content.Server.Atmos;

namespace Content.Server.Atmos.Reactions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class HydrogenFireReaction : IGasReactionEffect
    {
        public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem, float heatScale)
        {
            var energyReleased = 0f;
            var oldHeatCapacity = atmosphereSystem.GetHeatCapacity(mixture, true);
            var temperature = mixture.Temperature;

            if (holder is not TileAtmosphere location)
                return ReactionResult.NoReaction;

            mixture.ReactionResults[(byte)GasReaction.Fire] = 0f;
            var burnedFuel = 0f;
            var initialHydrogen = mixture.GetMoles(Gas.Hydrogen);

            if (mixture.GetMoles(Gas.Oxygen) < initialHydrogen ||
                Atmospherics.MinimumHydrogenOxyburnEnergy > (temperature * oldHeatCapacity * heatScale))
            {
                burnedFuel = mixture.GetMoles(Gas.Oxygen) / Atmospherics.HydrogenBurnOxyFactor;
                if (burnedFuel > initialHydrogen)
                    burnedFuel = initialHydrogen;

                mixture.AdjustMoles(Gas.Hydrogen, -burnedFuel);
            }
            else
            {
                burnedFuel = initialHydrogen;
                mixture.SetMoles(Gas.Hydrogen, mixture.GetMoles(Gas.Hydrogen ) * (1 - 1 / Atmospherics.HydrogenBurnHydrFactor));
                mixture.AdjustMoles(Gas.Oxygen, -mixture.GetMoles(Gas.Hydrogen));
                energyReleased += (Atmospherics.FireHydrogenEnergyReleased * burnedFuel * (Atmospherics.HydrogenBurnHydrFactor - 1));
            }

            if (burnedFuel > 0)
            {
                energyReleased += (Atmospherics.FireHydrogenEnergyReleased * burnedFuel);

                // TODO ATMOS Radiation pulse here!

                // Conservation of mass is important.
                mixture.AdjustMoles(Gas.WaterVapor, burnedFuel);

                mixture.ReactionResults[(byte)GasReaction.Fire] += burnedFuel;
            }

            energyReleased /= heatScale; // adjust energy to make sure speedup doesn't cause mega temperature rise
            if (energyReleased > 0)
            {
                var newHeatCapacity = atmosphereSystem.GetHeatCapacity(mixture, true);
                if (newHeatCapacity > Atmospherics.MinimumHeatCapacity)
                    mixture.Temperature = ((temperature * oldHeatCapacity + energyReleased) / newHeatCapacity);
            }

            if (location != null)
            {
                temperature = mixture.Temperature;
                if (temperature > Atmospherics.FireMinimumTemperatureToExist)
                {
                    atmosphereSystem.HotspotExpose(location, temperature, mixture.Volume);
                }
            }

            return mixture.ReactionResults[(byte)GasReaction.Fire] != 0 ? ReactionResult.Reacting : ReactionResult.NoReaction;
        }
    }
}
