using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared._CorvaxGoob.BookOfGreentext;

public sealed partial class WashCurseOfGreentextReactionSystem
    : EntityEffectSystem<CurseOfBookOfGreentextComponent, WashCurseOfGreentextReaction>
{
    protected override void Effect(Entity<CurseOfBookOfGreentextComponent> entity,
        ref EntityEffectEvent<WashCurseOfGreentextReaction> args)
    {
        RemComp<CurseOfBookOfGreentextComponent>(entity);
    }
}

public sealed partial class WashCurseOfGreentextReaction
    : EntityEffectBase<WashCurseOfGreentextReaction>
{
    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-wash-curse-of-greentext-reaction", ("chance", Probability));
}
