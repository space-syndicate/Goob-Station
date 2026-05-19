using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Body;
using Content.Shared.Chemistry.ReactionEffects;
using Content.Shared.EntityEffects;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Imperial.ChemistryRework;


public sealed partial class ChangeMarkingColorEntityEffectSystem : EntityEffectSystem<VisualBodyComponent, ChangeMarkingColor>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedVisualBodySystem _visualBody = default!;
    [Dependency] private readonly MarkingManager _marking = default!;


    protected override void Effect(Entity<VisualBodyComponent> entity, ref EntityEffectEvent<ChangeMarkingColor> args)
    {
        if (!_visualBody.TryGatherMarkingsData(entity.Owner, null, out var profiles, out _, out var applied))
            return;

        if (args.Effect.MarkingCategory == ChemicalMarkingCategory.Skin)
        {
            ApplySkinColor(entity.Owner, profiles, args);
            return;
        }

        if (!TryGetLayer(args.Effect.MarkingCategory, out var layer) || applied == null)
            return;

        var updated = new Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>>();

        foreach (var (category, organMarkings) in applied)
        {
            if (!organMarkings.TryGetValue(layer, out var markings) || markings.Count == 0)
                continue;

            var clone = organMarkings.ToDictionary(
                kvp => kvp.Key,
                kvp => new List<Marking>(kvp.Value));

            if (!TryRecolorMarking(clone[layer][0], args, out var recolored))
                continue;

            clone[layer][0] = recolored;
            _marking.EnsureValidColors(clone);
            updated[category] = clone;
        }

        if (updated.Count > 0)
            _visualBody.ApplyMarkings(entity.Owner, updated);
    }

    private void ApplySkinColor(
        EntityUid body,
        Dictionary<ProtoId<OrganCategoryPrototype>, OrganProfileData> profiles,
        EntityEffectEvent<ChangeMarkingColor> args)
    {
        var color = args.Effect.InvertColor
            ? InvertSkinColor(profiles, args)
            : GenerateColor(args.Effect.PaintingColor);

        var updated = new Dictionary<ProtoId<OrganCategoryPrototype>, OrganProfileData>();

        foreach (var (category, profile) in profiles)
        {
            var clone = profile;
            clone.SkinColor = color;
            updated[category] = clone;
        }

        if (updated.Count > 0)
            _visualBody.ApplyProfiles(body, updated);
    }

    private bool TryRecolorMarking(
        Marking marking,
        EntityEffectEvent<ChangeMarkingColor> args,
        out Marking recolored)
    {
        recolored = marking;

        if (!_marking.TryGetMarking(marking, out var proto))
            return false;

        if (marking.MarkingColors.Count != proto.Sprites.Count)
            marking = new Marking(marking.MarkingId, proto.Sprites.Count);

        var color = args.Effect.InvertColor
            ? InvertMarkingColor(marking)
            : GenerateColor(args.Effect.PaintingColor);

        recolored = marking.WithColor(EnsureVisible(color));
        return true;
    }

    private Color InvertMarkingColor(Marking marking)
    {
        if (marking.MarkingColors.Count > 0)
            return Invert(marking.MarkingColors[0]);

        return GenerateColor(null);
    }

    private Color InvertSkinColor(
        Dictionary<ProtoId<OrganCategoryPrototype>, OrganProfileData> profiles,
        EntityEffectEvent<ChangeMarkingColor> args)
    {
        foreach (var profile in profiles.Values)
            return Invert(profile.SkinColor);

        return GenerateColor(args.Effect.PaintingColor);
    }

    private Color GenerateColor(Color? paintingColor = null)
    {
        if (paintingColor != null)
            return EnsureVisible(paintingColor.Value);

        var r = _random.NextByte(255);
        var g = _random.NextByte(255);
        var b = _random.NextByte(255);

        return new Color(r, g, b);
    }

    /// <summary>
    /// Inverts RGB only. Inverting alpha makes markings fully transparent on the sprite layer.
    /// </summary>
    private static Color Invert(Color color)
    {
        return new Color(1f - color.R, 1f - color.G, 1f - color.B, color.A);
    }

    private static Color EnsureVisible(Color color)
    {
        if (color.A > 0.01f)
            return color;

        return color with { A = 1f };
    }

    private static bool TryGetLayer(
        ChemicalMarkingCategory category,
        [NotNullWhen(true)] out HumanoidVisualLayers layer)
    {
        return Enum.TryParse(category.ToString(), out layer);
    }
}
