using Content.Shared.Body.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Server.Imperial.Medieval.EntityEffects;


public sealed partial class AddReagentToTargetEntityEffectSystem : EntityEffectSystem<BloodstreamComponent, AddReagentToTarget>
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;

    protected override void Effect(Entity<BloodstreamComponent> entity, ref EntityEffectEvent<AddReagentToTarget> args)
    {
        if (!_solutionContainerSystem.TryGetSolution(entity.Owner, entity.Comp.ChemicalSolutionName, out var injectorSolution)) return;

        foreach (var reagent in args.Effect.Reagents)
            injectorSolution.Value.Comp.Solution.AddReagent(reagent.Reagent, reagent.Quantity);
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
