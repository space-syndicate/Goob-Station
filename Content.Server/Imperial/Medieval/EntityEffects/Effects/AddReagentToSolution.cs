using Content.Server.Body.Components;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Server.Imperial.Medieval.EntityEffects;


public sealed partial class AddReagentToTarget : EntityEffect
{
    /// <summary>
    ///     The reagent ID to remove. Only one of this and <see cref="Group"/> should be active.
    /// </summary>
    [DataField(required: true)]
    public List<ReagentQuantity> Reagents = new();

    public override void Effect(EntityEffectBaseArgs args)
    {
        var solutionContainerSystem = args.EntityManager.System<SharedSolutionContainerSystem>();

        if (!args.EntityManager.TryGetComponent<BloodstreamComponent>(args.TargetEntity, out var bloodstreamComponent)) return;
        if (!solutionContainerSystem.TryGetSolution(args.TargetEntity, bloodstreamComponent.ChemicalSolutionName, out var injectorSolution)) return;

        foreach (var reagent in Reagents)
            injectorSolution.Value.Comp.Solution.AddReagent(reagent.Reagent, reagent.Quantity);
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) => "";
}
