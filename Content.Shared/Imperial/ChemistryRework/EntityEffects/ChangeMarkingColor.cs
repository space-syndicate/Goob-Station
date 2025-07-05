using Content.Shared.EntityEffects;
using Content.Shared.Humanoid;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.Chemistry.ReactionEffects;

/// <summary>
///     Changes the mark of the entity layer. Also able to change the color of her skin.
/// </summary>
public sealed partial class ChangeMarkingColor : EventEntityEffect<ChangeMarkingColor>
{
    /// <summary>
    ///     Includes all enums from <see cref="Shared.Humanoid.Markings.MarkingCategories"/> and Skin.
    /// </summary>
    [DataField("markingCategory")]
    public string MarkingCategory = "Hair";

    /// <summary>
    ///     This option ignores the <see cref="PaintingColor"/> parameter and inverts the color.
    /// </summary>
    [DataField("invertColor")]
    public bool InvertColor = false;

    /// <summary>
    ///    If no color was passed in and <see cref="InvertColor"/> is set to false, then generates a random color every metabolization cycle.
    /// </summary>
    [DataField("color")]
    public string? PaintingColor;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("reagent-effect-guidebook-change-marking-color",
            ("chance", Probability),
            ("category", MarkingCategory)
        );

    public Color GenerateColor()
    {
        if (PaintingColor != null) return Color.FromHex(PaintingColor);

        var random = IoCManager.Resolve<IRobustRandom>();

        var r = random.NextByte(255);
        var g = random.NextByte(255);
        var b = random.NextByte(255);

        return new Color(r, g, b);
    }

    public Color InvertMarkingColor(EntityEffectBaseArgs args, Shared.Humanoid.Markings.MarkingCategories marking)
    {
        if (
            !args.EntityManager.TryGetComponent<HumanoidAppearanceComponent>(args.TargetEntity, out var hum)
        ) return GenerateColor();

        if (!hum.MarkingSet.TryGetCategory(marking, out var markings) || MarkingCategory == "Skin")
        {
            if (MarkingCategory != "Skin") return GenerateColor();

            return Invert(hum.SkinColor);
        }

        foreach (var mark in markings)
            return Invert(mark.MarkingColors[0]);

        return GenerateColor();
    }

    public Color Invert(Color color)
    {
        if (!SkinColor.VerifySkinColor(HumanoidSkinColor.HumanToned, color)) return new Color(1 - color.R, 1 - color.G, 1 - color.B);

        return SkinColor.HumanSkinTone(
            (int)(100 - SkinColor.HumanSkinToneFromColor(color))
        );
    }
}
