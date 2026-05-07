using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Imperial.Power.Components;
using Content.Shared.Atmos;
using Content.Shared.Imperial.Power.Components;
using Content.Shared.Imperial.Power.Prototypes;
using Content.Shared.Radiation.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Imperial.Power.EntitySystems;

/// <summary>
/// Обрабатывает газовые эффекты суперматерии:
/// расход газов, самовосстановление и модификаторы радиации.
/// </summary>
public sealed class SupermatterGasSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _protoMan = default!;

    private readonly List<(int gasId, SupermatterGasReactionPrototype proto)> _reactions = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SupermatterGasComponent, ComponentInit>(OnSupermatterGasInit);
        SubscribeLocalEvent<SupermatterGasComponent, AtmosExposedUpdateEvent>(OnAtmosExposedUpdate);

        CacheReactionPrototypes();
    }

    private void CacheReactionPrototypes()
    {
        _reactions.Clear();

        foreach (var proto in _protoMan.EnumeratePrototypes<SupermatterGasReactionPrototype>())
        {
            if (!AtmosCommandUtils.TryParseGasID(proto.Gas, out var gasId))
            {
                Log.Error($"Supermatter gas reaction prototype '{proto.ID}' has invalid gas id '{proto.Gas}'.");
                continue;
            }

            _reactions.Add((gasId, proto));
        }

        _reactions.Sort((a, b) => b.proto.Priority.CompareTo(a.proto.Priority));
    }

    private void OnSupermatterGasInit(EntityUid uid, SupermatterGasComponent component, ComponentInit args)
    {
        EnsureComp<AtmosExposedComponent>(uid);
    }

    private void OnAtmosExposedUpdate(EntityUid uid, SupermatterGasComponent component, ref AtmosExposedUpdateEvent args)
    {
        component.CachedGasMixture = args.GasMixture;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var enumerator = EntityQueryEnumerator<SupermatterIntegrityComponent, SupermatterGasComponent>();
        while (enumerator.MoveNext(out var uid, out var integrity, out var gasComp))
        {
            var gas = gasComp.CachedGasMixture;
            if (gas == null)
                continue;

            gasComp.RuntimeRadiationMultiplier = 1f;
            gasComp.RuntimeLightningMultiplier = 1f;
            gasComp.RuntimeEventSpeedMultiplier = 1f;
            gasComp.RuntimeDisableTouchGib = false;

            foreach (var (gasId, proto) in _reactions)
            {
                if (!integrity.Activated && !proto.ConsumeWhenInactive)
                    continue;

                var moles = gas.GetMoles(gasId);
                var excess = moles - gasComp.GasActivationMoles;
                if (excess <= 0f)
                    continue;

                gas.AdjustMoles(gasId, -gasComp.GasConsumptionPerSecond);
            }

            ApplyGasEffects((uid, integrity), (uid, gasComp), gas, frameTime);
        }
    }

    private void ApplyGasEffects(
        Entity<SupermatterIntegrityComponent> integrity,
        Entity<SupermatterGasComponent> gasComp,
        GasMixture gas,
        float frameTime)
    {
        var entMan = EntityManager;
        var sysMan = EntityManager.EntitySysManager;

        foreach (var (gasId, proto) in _reactions)
        {
            var active = gas.GetMoles(gasId) > gasComp.Comp.GasActivationMoles;
            if (!active && !proto.ProcessWhenBelowThreshold)
                continue;

            if (!integrity.Comp.Activated && !proto.AppliesWhenInactive)
                continue;

            proto.Reaction.React(integrity.Owner, integrity.Comp, gasComp.Comp, gas, frameTime, entMan, sysMan, active);
        }

        if (TryComp(integrity, out RadiationSourceComponent? radiation))
        {
            float baseIntensity = radiation.Intensity;
            
            if (TryComp<SupermatterEventComponent>(integrity, out var eventComp))
            {
                if (eventComp.CurrentEvent != SupermatterEventComponent.SupermatterEventType.Radiation
                    || eventComp.EventEndTime == TimeSpan.Zero)
                {
                    baseIntensity = eventComp.DefaultRadiationIntensity;
                }
            }
            radiation.Intensity = baseIntensity * gasComp.Comp.RuntimeRadiationMultiplier;
        }
    }
}


