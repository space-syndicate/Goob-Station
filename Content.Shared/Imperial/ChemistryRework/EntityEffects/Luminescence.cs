using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.Chemistry.ReactionEffects;

/// <summary>
///     Causes the entity to glow.
/// </summary>
/// <remarks>
///     Since I made a crutch again, it does not work when applying the essence to the skin.
/// </remarks>
public sealed partial class Luminescence : EntityEffect
{
    /// <summary>
    ///     If the color was not transferred, then generates a random color every metabolization cycle
    /// </summary>
    [DataField("color")]
    public string? PaintingColor;

    [DataField("minEnergy")]
    public float MinEnergy = 2;

    [DataField("maxEnergy")]
    public float MaxEnergy = float.PositiveInfinity;

    [DataField("minRange")]
    public float MinRange = 2;

    [DataField("maxRange")]
    public float MaxRange = float.PositiveInfinity;

    [DataField("rangePerUnit")]
    public float RangePerUnit = 0.1f;

    [DataField("energyPerUnit")]
    public float EnergyPerUnit = 0.1f;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("reagent-effect-guidebook-luminescence",
            ("chance", Probability)
        );

    public override void Effect(EntityEffectBaseArgs args)
    {
        if (args is not EntityEffectReagentArgs reagentArgs) return;

        var pointLightSystem = reagentArgs.EntityManager.System<SharedPointLightSystem>();

        // If the next time the reagent metabolization ends, we turn off the glow.

        var totalReagentCount = GetReagentCount(reagentArgs);

        if (totalReagentCount - reagentArgs.Quantity <= FixedPoint2.Zero)
        {
            if (pointLightSystem.TryGetLight(reagentArgs.TargetEntity, out var lightConp))
                pointLightSystem.SetEnabled(reagentArgs.TargetEntity, false, lightConp);

            return;
        }

        // If the light source has already been applied to nature, then we simply change its parameters, rather than creating new ones.

        if (pointLightSystem.TryGetLight(reagentArgs.TargetEntity, out var existLight))
        {
            if (!existLight.Enabled) pointLightSystem.SetEnabled(reagentArgs.TargetEntity, true, existLight);

            SetLightColor(reagentArgs, existLight);
            ScaleLightPower(reagentArgs, existLight);

            return;
        }

        // Add point light to the entity

        var light = pointLightSystem.EnsureLight(reagentArgs.TargetEntity);

        SetLightColor(reagentArgs, light);
        ScaleLightPower(reagentArgs, light);

        pointLightSystem.SetEnabled(reagentArgs.TargetEntity, true, light);
    }

    private static Color GenerateRandomColor()
    {
        var random = IoCManager.Resolve<IRobustRandom>();

        var r = random.NextByte(255);
        var g = random.NextByte(255);
        var b = random.NextByte(255);

        return new Color(r, g, b);
    }

    private void SetLightColor(EntityEffectReagentArgs args, SharedPointLightComponent light)
    {
        var pointLightSystem = args.EntityManager.System<SharedPointLightSystem>();

        if (PaintingColor == null)
            pointLightSystem.SetColor(args.TargetEntity, GenerateRandomColor(), light);
        else
            pointLightSystem.SetColor(args.TargetEntity, Color.FromHex(PaintingColor), light);
    }

    private void ScaleLightPower(EntityEffectReagentArgs args, SharedPointLightComponent light)
    {
        var pointLightSystem = args.EntityManager.System<SharedPointLightSystem>();
        var reagentCount = GetReagentCount(args);

        var energy = MathF.Max(
            MathF.Min((float)(reagentCount * EnergyPerUnit), MaxEnergy),
            MinEnergy
        );
        var range = MathF.Max(
            MathF.Min((float)(reagentCount * RangePerUnit), MaxEnergy),
            MinRange
        );

        pointLightSystem.SetEnergy(args.TargetEntity, energy, light);
        pointLightSystem.SetRadius(args.TargetEntity, range, light);
    }

    private static FixedPoint2 GetReagentCount(EntityEffectReagentArgs args)
    {
        if (args.Source != null && args.Reagent?.ID != null) return args.Source!.GetTotalPrototypeQuantity(args.Reagent!.ID);

        return FixedPoint2.Zero;
    }
}
