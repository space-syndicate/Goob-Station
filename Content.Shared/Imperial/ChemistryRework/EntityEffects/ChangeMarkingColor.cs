using System.Numerics;
using Content.Shared.EntityEffects;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.Chemistry.ReactionEffects;

/// <summary>
///     Changes the mark of the entity layer. Also able to change the color of her skin.
/// </summary>
public sealed partial class ChangeMarkingColor : EntityEffectBase<ChangeMarkingColor>
{
    /// <summary>
    ///     Includes all enums from <see cref="Shared.Humanoid.Markings.MarkingCategories"/> and Skin.
    /// </summary>
    [DataField]
    public ChemicalMarkingCategory MarkingCategory = ChemicalMarkingCategory.Hair;

    /// <summary>
    ///     This option ignores the <see cref="PaintingColor"/> parameter and inverts the color.
    /// </summary>
    [DataField]
    public bool InvertColor = false;

    /// <summary>
    ///    If no color was passed in and <see cref="InvertColor"/> is set to false, then generates a random color every metabolization cycle.
    /// </summary>
    [DataField]
    public Color? PaintingColor;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("reagent-effect-guidebook-change-marking-color",
            ("chance", Probability),
            ("category", MarkingCategory)
        );
}

