using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Imperial.Power.Components;
using Content.Server.Imperial.XxRaay.Systems.Supermatter;
using Content.Shared.Atmos;
using Content.Shared.Imperial.XxRaay.Supermatter;
using Content.Shared.Radiation.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Imperial.Power.EntitySystems;

/// <summary>
/// Обрабатывает газовые эффекты суперматерии:
/// расход газов, самовосстановление и модификаторы радиации.
/// </summary>
public sealed class SupermatterGasSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private readonly Dictionary<Gas, List<ISupermatterGasReaction>> _reactionsByGas = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SupermatterGasComponent, ComponentInit>(OnSupermatterGasInit);
        SubscribeLocalEvent<SupermatterGasComponent, AtmosExposedUpdateEvent>(OnAtmosExposedUpdate);

        LoadReactions();
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

            var antiNobMoles = gas.GetMoles(Gas.AntiNoblium);
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

            var thermMoles = gas.GetMoles(Gas.Thermonium);
            var ozoneMoles = gas.GetMoles(Gas.Ozonium);
            var plasmaMoles = gas.GetMoles(Gas.Plasma);
            var hyperNobMoles = gas.GetMoles(Gas.HyperNoblium);

            var thermActive = thermMoles > gasComp.GasActivationMoles;
            var ozoneActive = ozoneMoles > gasComp.GasActivationMoles;
            var plasmaActive = plasmaMoles > gasComp.GasActivationMoles;
            var hyperNobActive = hyperNobMoles > gasComp.GasActivationMoles;

            gasComp.GasTickAccumulator += TimeSpan.FromSeconds(frameTime);

            var wholeSeconds = (int) gasComp.GasTickAccumulator.TotalSeconds;
            if (wholeSeconds > 0)
            {
                gasComp.GasTickAccumulator -= TimeSpan.FromSeconds(wholeSeconds);
                var consumption = gasComp.GasConsumptionPerSecond * wholeSeconds;

                if (antiNobActive)
                    gas.AdjustMoles(Gas.AntiNoblium, -consumption);

                if (integrity.Activated)
                {
                    if (thermActive)
                        gas.AdjustMoles(Gas.Thermonium, -consumption);
                    if (ozoneActive)
                        gas.AdjustMoles(Gas.Ozonium, -consumption);
                    if (plasmaActive)
                        gas.AdjustMoles(Gas.Plasma, -consumption);
                    if (hyperNobActive)
                        gas.AdjustMoles(Gas.HyperNoblium, -consumption);
                }
            }

            if (!integrity.Activated)
                continue;

            ApplyGasEffects((uid, integrity), (uid, gasComp), gas, frameTime);
        }
    }

    private void LoadReactions()
    {
        _reactionsByGas.Clear();

        foreach (var proto in _prototypeManager.EnumeratePrototypes<SupermatterGasReactionPrototype>())
        {
            if (!typeof(ISupermatterGasReaction).IsAssignableFrom(proto.Reaction))
                continue;

            if (Activator.CreateInstance(proto.Reaction) is not ISupermatterGasReaction reaction)
                continue;

            if (!_reactionsByGas.TryGetValue(proto.Gas, out var list))
            {
                list = new List<ISupermatterGasReaction>();
                _reactionsByGas[proto.Gas] = list;
            }

            list.Add(reaction);
        }
    }

    private void ApplyGasEffects(
        Entity<SupermatterIntegrityComponent> integrity,
        Entity<SupermatterGasComponent> gasComp,
        GasMixture gas,
        float frameTime)
    {
        foreach (var (gasId, reactions) in _reactionsByGas)
        {
            foreach (var reaction in reactions)
            {
                reaction.React(this, integrity, gasComp, gas, gasId, frameTime);
            }
        }
    }

    public bool TryGetComponent<T>(EntityUid uid, out T? component) where T : IComponent
    {
        return EntityManager.TryGetComponent(uid, out component);
    }
}


