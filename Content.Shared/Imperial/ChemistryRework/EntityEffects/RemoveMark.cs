using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared.Chemistry.ReactionEffects;

/// <summary>
///     Remove part of an entity.
/// </summary>
public sealed partial class RemoveMark : EventEntityEffect<RemoveMark>
{
    /// <summary>
    ///     All types of marks can be seen in <see cref="Shared.Humanoid.Markings.MarkingCategories"/>.
    /// </summary>
    [DataField("MarkingCategory")]
    public string MarkingCategory = "Hair";

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("reagent-effect-guidebook-remove-mark",
            ("chance", Probability),
            ("category", MarkingCategory)
        );
}
