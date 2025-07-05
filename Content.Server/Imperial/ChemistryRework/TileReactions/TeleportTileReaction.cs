using System.Numerics;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Maps;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.Chemistry.ReactionEffects;

[DataDefinition]
public sealed partial class TeleportTileReaction : ITileReaction
{
    [DataField("radiusPerUnit")]
    public float RadiusPerUnit = 1;

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

    [DataField("teleportType")]
    public TeleportTypes? TeleportType;

    [DataField("coordinates")]
    public Vector2? Coordinates;

    public FixedPoint2 TileReact(TileRef tile, ReagentPrototype reagent, FixedPoint2 reactVolume, IEntityManager entityManager, List<ReagentData>? data = null)
    {
        var lookupSystem = entityManager.System<EntityLookupSystem>();
        var xformSystem = entityManager.System<TransformSystem>();
        var trufSys = entityManager.System<TurfSystem>();

        var energy = MathF.Max(
            MathF.Min((float) (reactVolume * EnergyPerUnit), MaxEnergy),
            MinEnergy
        );
        var range = MathF.Max(
            MathF.Min((float) (reactVolume * RadiusPerUnit), MaxRange),
            MinRange
        );

        var entities = lookupSystem.GetEntitiesInRange(trufSys.GetTileCenter(tile), range, LookupFlags.Dynamic);

        foreach (var entity in entities)
        {
            var mapPosition = xformSystem.GetWorldPosition(entity);
            var reactionBounds = new Box2(mapPosition - new Vector2(energy, energy), mapPosition + new Vector2(energy, energy));

            var newPosition = Coordinates;

            if (TeleportType == TeleportTypes.Random)
                newPosition = GetRandomCoords(reactionBounds);
            else if (TeleportType == TeleportTypes.FaceRotation)
                newPosition = GetPositionFromRotation(reactionBounds, energy, entity);

            if (newPosition != null)
                xformSystem.SetWorldPosition(
                    entity,
                    (Vector2) newPosition
                );
        }

        return reactVolume;
    }

    private static Vector2 GetRandomCoords(Box2 reactionBounds)
    {
        var random = IoCManager.Resolve<IRobustRandom>();

        var randomX = random.NextFloat(reactionBounds.Left, reactionBounds.Right);
        var randomY = random.NextFloat(reactionBounds.Bottom, reactionBounds.Top);

        return new Vector2(randomX, randomY);
    }

    private static Vector2 GetPositionFromRotation(Box2 reactionBounds, float energy, EntityUid uid)
    {
        var entityManager = IoCManager.Resolve<IEntityManager>();

        var xformSystem = entityManager.System<TransformSystem>();

        var resultVector = Angle.FromDegrees(45).RotateVec(
            xformSystem.GetWorldRotation(uid).RotateVec(new Vector2(energy, energy))
        );

        return reactionBounds.Center - resultVector;
    }
}
