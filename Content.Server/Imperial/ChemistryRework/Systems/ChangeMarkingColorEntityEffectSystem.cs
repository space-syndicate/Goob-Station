using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Server.Humanoid;
using Content.Shared.Chemistry.ReactionEffects;
using Content.Shared.EntityEffects;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Random;

namespace Content.Server.Chemistry.ReactionEffects;


public sealed partial class ChangeMarkingColortEntityEffectSystem : EntityEffectSystem<HumanoidAppearanceComponent, ChangeMarkingColor>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoidAppearanceSystem = default!;


    protected override void Effect(Entity<HumanoidAppearanceComponent> entity, ref EntityEffectEvent<ChangeMarkingColor> args)
    {
        var color = args.Effect.InvertColor ? InvertMarkingColor(entity, args) : GenerateColor(args.Effect.PaintingColor);

        if (!TryParseMarkingCategory(args.Effect.MarkingCategory, out var markingCategory))
            _humanoidAppearanceSystem.SetSkinColor(entity, color);
        else
            _humanoidAppearanceSystem.SetMarkingColor(entity, markingCategory.Value, 0, [color]);
    }

    #region Helpers

    private Color GenerateColor(Color? paintingColor = null)
    {
        if (paintingColor != null) return paintingColor.Value;

        var r = _random.NextByte(255);
        var g = _random.NextByte(255);
        var b = _random.NextByte(255);

        return new Color(r, g, b);
    }

    private Color InvertMarkingColor(Entity<HumanoidAppearanceComponent> entity, EntityEffectEvent<ChangeMarkingColor> args)
    {
        if (!TryParseMarkingCategory(args.Effect.MarkingCategory, out var markingCategory))
            return Invert(entity.Comp.SkinColor);

        if (!entity.Comp.MarkingSet.TryGetCategory(markingCategory.Value, out var markings))
            return GenerateColor(args.Effect.PaintingColor);

        foreach (var mark in markings)
            return Invert(mark.MarkingColors[0]);

        return GenerateColor(args.Effect.PaintingColor);
    }

    private Color Invert(Color color)
    {
        return new Color(new Vector4(1.0f) - color.RGBA);
    }

    private bool TryParseMarkingCategory(ChemicalMarkingCategory chemicalMarkingCategory, [NotNullWhen(true)] out MarkingCategories? markingCategory)
    {
        markingCategory = null;

        if (Enum.TryParse<MarkingCategories>(chemicalMarkingCategory.ToString(), out var categories))
            markingCategory = categories;

        return chemicalMarkingCategory != ChemicalMarkingCategory.Skin;
    }

    #endregion
}
