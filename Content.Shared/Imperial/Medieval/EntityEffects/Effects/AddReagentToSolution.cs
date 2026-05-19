using Content.Shared.Body.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Server.Imperial.Medieval.EntityEffects;


public sealed partial class AddReagentToTargetEntityEffectSystem : EntityEffectSystem<BloodstreamComponent, AddReagentToTarget>
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;

    protected override void Effect(Entity<BloodstreamComponent> entity, ref EntityEffectEvent<AddReagentToTarget> args)
    {
        if (!_solutionContainerSystem.TryGetSolution(entity.Owner, entity.Comp.MetabolitesSolutionName, out var injectorSolution)) return;

        foreach (var reagent in args.Effect.Reagents)
            _solutionContainerSystem.TryAddReagent(injectorSolution.Value, reagent, out var _);
    }
}


/// <summary>
///     Explodes the body
/// </summary>
public sealed partial class AddReagentToTarget : EntityEffectBase<AddReagentToTarget>
{
    /// <summary>
    ///     The reagent ID to remove. Only one of this and <see cref="Group"/> should be active.
    /// </summary>
    [DataField(required: true)]
    public List<ReagentQuantity> Reagents = new();


    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) => "";
}
