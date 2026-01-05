using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Imperial.Power.Components;
using Content.Shared.Atmos;
using Content.Shared.Radiation.Components;

namespace Content.Server.Imperial.Power.EntitySystems;

/// <summary>
/// Обрабатывает газовые эффекты суперматерии:
/// расход газов, самовосстановление и модификаторы радиации.
/// </summary>
public sealed class SupermatterGasSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var enumerator = EntityQueryEnumerator<SupermatterIntegrityComponent, SupermatterGasComponent, TransformComponent>();
        while (enumerator.MoveNext(out var uid, out var integrity, out var gasComp, out var xform))
        {
            var gas = _atmosphereSystem.GetContainingMixture((uid, xform), true, true);
            if (gas == null)
                continue;

            var antiNobMoles = gas.GetMoles(gasComp.AntiNobliumGas);
            var antiNobActive = antiNobMoles > gasComp.GasActivationMoles;

            if (gasComp.AntiNobliumHardShutdownEnabled && antiNobActive)
            {
                integrity.Activated = false;
                gasComp.WasShutdownByAntiNoblium = true;
            }
            else if (gasComp.AntiNobliumHardShutdownEnabled && !antiNobActive && gasComp.WasShutdownByAntiNoblium)
            {
                integrity.Activated = true;
                gasComp.WasShutdownByAntiNoblium = false;
            }

            var thermMoles = gas.GetMoles(gasComp.ThermoniumGas);
            var ozoneMoles = gas.GetMoles(gasComp.OzoneGas);
            var plasmaMoles = gas.GetMoles(gasComp.PlasmaGas);
            var hyperNobMoles = gas.GetMoles(gasComp.HyperNobliumGas);

            var thermActive = thermMoles > gasComp.GasActivationMoles;
            var ozoneActive = ozoneMoles > gasComp.GasActivationMoles;
            var plasmaActive = plasmaMoles > gasComp.GasActivationMoles;
            var hyperNobActive = hyperNobMoles > gasComp.GasActivationMoles;

            gasComp.GasTickAccumulator += TimeSpan.FromSeconds(frameTime);
            
            while (gasComp.GasTickAccumulator >= TimeSpan.FromSeconds(1))
            {
                var currentAntiNobMoles = gas.GetMoles(gasComp.AntiNobliumGas);
                if (currentAntiNobMoles > gasComp.GasActivationMoles)
                {
                    gas.AdjustMoles(gasComp.AntiNobliumGas, -gasComp.GasConsumptionPerSecond);
                }

                if (integrity.Activated)
                {
                    var currentThermMoles = gas.GetMoles(gasComp.ThermoniumGas);
                    var currentOzoneMoles = gas.GetMoles(gasComp.OzoneGas);
                    var currentPlasmaMoles = gas.GetMoles(gasComp.PlasmaGas);
                    var currentHyperNobMoles = gas.GetMoles(gasComp.HyperNobliumGas);

                    if (currentThermMoles > gasComp.GasActivationMoles)
                        gas.AdjustMoles(gasComp.ThermoniumGas, -gasComp.GasConsumptionPerSecond);
                    if (currentOzoneMoles > gasComp.GasActivationMoles)
                        gas.AdjustMoles(gasComp.OzoneGas, -gasComp.GasConsumptionPerSecond);
                    if (currentPlasmaMoles > gasComp.GasActivationMoles)
                        gas.AdjustMoles(gasComp.PlasmaGas, -gasComp.GasConsumptionPerSecond);
                    if (currentHyperNobMoles > gasComp.GasActivationMoles)
                        gas.AdjustMoles(gasComp.HyperNobliumGas, -gasComp.GasConsumptionPerSecond);
                }

                gasComp.GasTickAccumulator -= TimeSpan.FromSeconds(1);
            }

            if (!integrity.Activated)
                continue;

            if (thermActive)
            {
                var regen = gasComp.ThermoniumIntegrityRegenPerSecond * frameTime;
                integrity.Integrity = MathF.Min(integrity.MaxIntegrity, integrity.Integrity + regen);
            }

            if (TryComp(uid, out RadiationSourceComponent? radiation))
            {
                float baseIntensity = radiation.Intensity;
                
                if (TryComp<SupermatterEventComponent>(uid, out var eventComp))
                {
                    if (eventComp.CurrentEvent != SupermatterEventComponent.SupermatterEventType.Radiation
                        || eventComp.EventEndTime == TimeSpan.Zero)
                    {
                        baseIntensity = eventComp.DefaultRadiationIntensity;
                    }
                }
                var multiplier = 1f;
                if (ozoneActive)
                    multiplier *= gasComp.OzoneRadiationMultiplier;
                if (plasmaActive)
                    multiplier *= gasComp.PlasmaRadiationMultiplier;

                radiation.Intensity = baseIntensity * multiplier;
            }
        }
    }
}


