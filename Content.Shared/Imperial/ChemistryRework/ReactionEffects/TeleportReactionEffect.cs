using System.Numerics;
using Content.Shared.Chemistry;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.Chemistry.ReactionEffects;

/// <summary>
///     Teleports a entity within a certain radius from the epicenter to X coordinates.
/// </summary>
public sealed partial class Teleport : EntityEffect
{
    [DataField("radiusPerUnit")]
    public float RadiusPerUnit = 0;

    [DataField("energyPerUnit")]
    public float EnergyPerUnit = 1;

    [DataField("minEnergy")]
    public float MinEnergy = 1;

    [DataField("maxEnergy")]
    public float MaxEnergy = float.MaxValue;

    [DataField("minRange")]
    public float MinRange = 1;

    [DataField("maxRange")]
    public float MaxRange = float.MaxValue;

    /// <summary>
    ///     Random or FaceRotation.
    ///     FaceRotation - teleportation along the direct sector of the view of the entity
    /// </summary>

    [DataField("teleportType")]
    public TeleportTypes? TeleportType;

    [DataField("coordinates")]
    public Vector2? Coordinates;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("reagent-effect-guidebook-teleport",
            ("chance", Probability)
        );

    public override void Effect(EntityEffectBaseArgs args)
    {
        if (args is not EntityEffectReagentArgs reagentArgs) return;

        var lookupSystem = args.EntityManager.System<EntityLookupSystem>();
        var xformSystem = args.EntityManager.System<SharedTransformSystem>();

        var energy = MathF.Max(
            MathF.Min((float) (reagentArgs.Quantity * EnergyPerUnit), MaxEnergy),
            MinEnergy
        );
        var range = MathF.Max(
            MathF.Min((float) (reagentArgs.Quantity * RadiusPerUnit), MaxRange),
            MinRange
        );

        var entities = lookupSystem.GetEntitiesInRange(reagentArgs.TargetEntity, range, LookupFlags.Dynamic);
        var mapPosition = xformSystem.GetWorldPosition(reagentArgs.TargetEntity);
        var reactionBounds = new Box2(mapPosition - new Vector2(energy, energy), mapPosition + new Vector2(energy, energy));

        foreach (var entity in entities)
        {
            var newPosition = Coordinates;

            if (TeleportType == TeleportTypes.Random)
                newPosition = GetRandomCoords(reactionBounds);
            else if (TeleportType == TeleportTypes.FaceRotation)
                newPosition = GetPositionFromRotation(args, reactionBounds, energy, entity);

            if (newPosition != null)
                xformSystem.SetWorldPosition(
                    entity,
                    (Vector2) newPosition
                );
        }
    }

    private static Vector2 GetRandomCoords(Box2 reactionBounds)
    {
        var random = IoCManager.Resolve<IRobustRandom>();

        var randomX = random.NextFloat(reactionBounds.Left, reactionBounds.Right);
        var randomY = random.NextFloat(reactionBounds.Bottom, reactionBounds.Top);

        return new Vector2(randomX, randomY);
    }

    private static Vector2 GetPositionFromRotation(EntityEffectBaseArgs args, Box2 reactionBounds, float energy, EntityUid uid)
    {
        var xformSystem = args.EntityManager.System<SharedTransformSystem>();

        var resultVector = Angle.FromDegrees(45).RotateVec(
            xformSystem.GetWorldRotation(uid).RotateVec(new Vector2(energy, energy))
        );

        return reactionBounds.Center - resultVector;
    }
}
