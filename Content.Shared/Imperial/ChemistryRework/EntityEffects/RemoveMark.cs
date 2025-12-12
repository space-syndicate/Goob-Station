using Content.Shared.EntityEffects;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Prototypes;

namespace Content.Shared.Chemistry.ReactionEffects;

/// <summary>
///     Remove part of an entity.
/// </summary>
public sealed partial class RemoveMark : EntityEffectBase<RemoveMark>
{
    /// <summary>
    ///     All types of marks can be seen in <see cref="Shared.Humanoid.Markings.MarkingCategories"/>.
    /// </summary>
    [DataField]
    public MarkingCategories MarkingCategory = MarkingCategories.Hair;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("reagent-effect-guidebook-remove-mark",
            ("chance", Probability),
            ("category", MarkingCategory)
        );
}
