using Content.Server.Singularity.EntitySystems;
using Content.Shared.Chemistry.ReactionEffects;
using Content.Shared.EntityEffects;
using Robust.Shared.Physics.Components;

namespace Content.Server.Imperial.ChemistryRework;


public sealed partial class GravityEntityEffectSystem : EntityEffectSystem<PhysicsComponent, GravityReactionEffect>
{
    [Dependency] private readonly GravityWellSystem _gravityWellSystem = default!;


    protected override void Effect(Entity<PhysicsComponent> entity, ref EntityEffectEvent<GravityReactionEffect> args)
    {
        var range = MathF.Min((float)(args.Scale * args.Effect.ImpulsePerUnit), args.Effect.MaxRange);

        _gravityWellSystem.GravPulse(
            entity,
            range,
            args.Effect.MinRange,
            args.Effect.BaseRadialDeltaV,
            args.Effect.BaseTangentialDeltaV
        );
    }
}
