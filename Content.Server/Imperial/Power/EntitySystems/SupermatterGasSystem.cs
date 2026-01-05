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

            var antiNobMoles = gas[(int) gasComp.AntiNobliumGas];
            var antiNobActive = antiNobMoles > gasComp.GasActivationMoles;

            if (gasComp.AntiNobliumHardShutdownEnabled && antiNobActive)
            {
                integrity.Activated = false;
            }

            if (!integrity.Activated)
                continue;

            var thermMoles = gas[(int) gasComp.ThermoniumGas];
            var ozoneMoles = gas[(int) gasComp.OzoneGas];
            var plasmaMoles = gas[(int) gasComp.PlasmaGas];
            var hyperNobMoles = gas[(int) gasComp.HyperNobliumGas];

            var thermActive = thermMoles > gasComp.GasActivationMoles;
            var ozoneActive = ozoneMoles > gasComp.GasActivationMoles;
            var plasmaActive = plasmaMoles > gasComp.GasActivationMoles;
            var hyperNobActive = hyperNobMoles > gasComp.GasActivationMoles;

            gasComp.GasTickAccumulator += TimeSpan.FromSeconds(frameTime);
            
            if (gasComp.GasTickAccumulator >= TimeSpan.FromSeconds(1))
            {
                var consumption = gasComp.GasConsumptionPerSecond;
                
                var gasesToConsume = new List<(bool active, Gas gasType)>
                {
                    (thermActive, gasComp.ThermoniumGas),
                    (ozoneActive, gasComp.OzoneGas),
                    (plasmaActive, gasComp.PlasmaGas),
                    (hyperNobActive, gasComp.HyperNobliumGas),
                    (antiNobActive, gasComp.AntiNobliumGas)
                };

                foreach (var (active, gasType) in gasesToConsume)
                {
                    if (active)
                        gas.AdjustMoles((int) gasType, -consumption);
                }

                gasComp.GasTickAccumulator -= TimeSpan.FromSeconds(1);
            }

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


