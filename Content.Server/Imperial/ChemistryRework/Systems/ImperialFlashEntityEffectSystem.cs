using Content.Server.Flash;
using Content.Shared.Chemistry.ReactionEffects;
using Content.Shared.EntityEffects;
using Robust.Server.GameObjects;

namespace Content.Server.Imperial.ChemistryRework;


public sealed partial class ImperialFlashEntityEffectSystem : EntityEffectSystem<TransformComponent, ImperialFlashReactionEffect>
{
    [Dependency] private readonly TransformSystem _transformSystem = default!;
    [Dependency] private readonly PointLightSystem _pointLightSystem = default!;
    [Dependency] private readonly FlashSystem _flashSystem = default!;


    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<ImperialFlashReactionEffect> args)
    {
        var uid = Spawn(args.Effect.FlashEffectPrototype, _transformSystem.GetMapCoordinates(entity));
        var range = 1f;

        _transformSystem.AttachToGridOrMap(uid);

        if (TryComp<PointLightComponent>(uid, out var pointLightComp))
            _pointLightSystem.SetRadius(uid, MathF.Max(1.1f, range), pointLightComp);

        if (args.Effect.SlowOnlyTarget)
        {
            _flashSystem.Flash(
                entity,
                null,
                null,
                args.Effect.MaxDuration,
                1.0f
            );

            return;
        }

        _flashSystem.FlashArea(
            entity,
            null,
            args.Effect.MaxRange,
            args.Effect.MaxDuration,
            args.Effect.SlowTo
        );
    }
}
