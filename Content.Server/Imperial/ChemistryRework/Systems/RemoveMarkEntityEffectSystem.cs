using Content.Server.Humanoid;
using Content.Shared.Chemistry.ReactionEffects;
using Content.Shared.EntityEffects;
using Content.Shared.Humanoid;

namespace Content.Server.Imperial.ChemistryRework;


public sealed partial class RemoveMarkEntityEffectSystem : EntityEffectSystem<HumanoidAppearanceComponent, RemoveMark>
{
    [Dependency] private readonly HumanoidAppearanceSystem _humanoidAppearanceSystem = default!;


    protected override void Effect(Entity<HumanoidAppearanceComponent> entity, ref EntityEffectEvent<RemoveMark> args)
    {
        _humanoidAppearanceSystem.RemoveMarking(entity, args.Effect.MarkingCategory, 0);
    }
}
