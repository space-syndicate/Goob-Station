using Content.Server.Atmos.Components;
using Content.Server.Imperial.Power.Components.EventComponents;
using Content.Server.Radiation.Systems;
using Content.Shared.Atmos;
using Content.Shared.Imperial.Power.Components;
using Content.Shared.Imperial.Power.Prototypes;
using Content.Shared.Radiation.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.Imperial.Power.EntitySystems;

/// <summary>
/// Обрабатывает газовые эффекты суперматерии:
/// расход газов, самовосстановление и модификаторы радиации.
/// </summary>
public sealed class SupermatterGasSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = null!;
    [Dependency] private readonly IPrototypeManager _protoMan = null!;
    [Dependency] private readonly RadiationSystem _radiationSystem = null!;

    private readonly List<(int gasId, SupermatterGasReactionPrototype proto)> _reactions = [];

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
        if (!TryComp(uid, out SupermatterIntegrityComponent? integrity))
            return;

        var now = _gameTiming.CurTime;
        var frameTime = component.LastAtmosUpdate == TimeSpan.Zero
            ? 0f
            : (float) (now - component.LastAtmosUpdate).TotalSeconds;

        component.LastAtmosUpdate = now;

        var gas = args.GasMixture;
        component.RuntimeRadiationMultiplier = 1f;
        component.RuntimeLightningMultiplier = 1f;
        component.RuntimeEventSpeedMultiplier = 1f;
        component.RuntimeDisableTouchGib = false;

        foreach (var (gasId, proto) in _reactions)
        {
            if (!integrity.Activated && !proto.ConsumeWhenInactive)
                continue;

            var moles = gas.GetMoles(gasId);
            var excess = moles - component.GasActivationMoles;
            if (excess <= 0f)
                continue;

            gas.AdjustMoles(gasId, -component.GasConsumptionPerSecond);
        }

        ApplyGasEffects((uid, integrity), (uid, component), gas, frameTime);
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

            foreach (var reaction in proto.Reactions)
            {
                reaction.React(integrity.Owner, integrity.Comp, gasComp.Comp, gas, frameTime, entMan, sysMan, active);
            }
        }

        if (!TryComp(integrity, out RadiationSourceComponent? radiation))
            return;

        var baseIntensity = radiation.Intensity;

        if (TryComp<SupermatterRadiationEventComponent>(integrity, out var eventComp))
        {
            if (_gameTiming.CurTime <= eventComp.EndTime)
                baseIntensity = eventComp.Intensity!.Value;
        }
        _radiationSystem.SetIntensity(integrity.Owner, baseIntensity * gasComp.Comp.RuntimeRadiationMultiplier);
    }
}


