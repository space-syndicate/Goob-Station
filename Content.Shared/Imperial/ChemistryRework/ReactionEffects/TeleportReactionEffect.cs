using System.Numerics;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.Chemistry.ReactionEffects;


public sealed partial class TeleportEntityEffectSystem : EntityEffectSystem<MetaDataComponent, Teleport>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookupSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;


    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<Teleport> args)
    {
        var energy = MathF.Max(
            MathF.Min(args.Scale * args.Effect.EnergyPerUnit, args.Effect.MaxEnergy),
            args.Effect.MinEnergy
        );
        var range = MathF.Max(
            MathF.Min(args.Scale * args.Effect.RadiusPerUnit, args.Effect.MaxRange),
            args.Effect.MinRange
        );

        var entities = _entityLookupSystem.GetEntitiesInRange(entity, range, LookupFlags.Dynamic);
        var mapPosition = _transformSystem.GetWorldPosition(entity);
        var reactionBounds = new Box2(mapPosition - new Vector2(energy, energy), mapPosition + new Vector2(energy, energy));

        foreach (var teleportEntity in entities)
        {
            var newPosition = args.Effect.Coordinates;

            if (args.Effect.TeleportType == TeleportTypes.Random)
                newPosition = GetRandomCoords(reactionBounds);
            else if (args.Effect.TeleportType == TeleportTypes.FaceRotation)
                newPosition = GetPositionFromRotation(args, reactionBounds, energy, teleportEntity);

            if (newPosition != null)
                _transformSystem.SetWorldPosition(
                    teleportEntity,
                    (Vector2)newPosition
                );
        }
    }

    private Vector2 GetRandomCoords(Box2 reactionBounds)
    {
        var randomX = _random.NextFloat(reactionBounds.Left, reactionBounds.Right);
        var randomY = _random.NextFloat(reactionBounds.Bottom, reactionBounds.Top);

        return new Vector2(randomX, randomY);
    }

    private Vector2 GetPositionFromRotation(EntityEffectEvent<Teleport> args, Box2 reactionBounds, float energy, EntityUid uid)
    {
        var resultVector = Angle.FromDegrees(45).RotateVec(
            _transformSystem.GetWorldRotation(uid).RotateVec(new Vector2(energy, energy))
        );

        return reactionBounds.Center - resultVector;
    }
}


/// <summary>
///     Teleports a entity within a certain radius from the epicenter to X coordinates.
/// </summary>
public sealed partial class Teleport : EntityEffectBase<Teleport>
{
    [DataField]
    public float RadiusPerUnit = 0;

    [DataField]
    public float EnergyPerUnit = 1;

    [DataField]
    public float MinEnergy = 1;

    [DataField]
    public float MaxEnergy = float.MaxValue;

    [DataField]
    public float MinRange = 1;

    [DataField]
    public float MaxRange = float.MaxValue;

    /// <summary>
    ///     Random or FaceRotation.
    ///     FaceRotation - teleportation along the direct sector of the view of the entity
    /// </summary>

    [DataField]
    public TeleportTypes? TeleportType;

    [DataField]
    public Vector2? Coordinates;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("reagent-effect-guidebook-teleport",
            ("chance", Probability)
        );
}
