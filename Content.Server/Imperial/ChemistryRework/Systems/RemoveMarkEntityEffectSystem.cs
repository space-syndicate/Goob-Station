using Content.Shared.Body;
using Content.Shared.Chemistry.ReactionEffects;
using Content.Shared.EntityEffects;
using Content.Shared.Humanoid;

namespace Content.Server.Imperial.ChemistryRework;


public sealed partial class RemoveMarkEntityEffectSystem : EntityEffectSystem<VisualBodyComponent, RemoveMark>
{


    protected override void Effect(Entity<VisualBodyComponent> entity, ref EntityEffectEvent<RemoveMark> args)
    {
        // _humanoidAppearanceSystem.RemoveMarking(entity, args.Effect.MarkingCategory, 0);
    }
}
