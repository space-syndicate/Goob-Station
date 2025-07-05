using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared.Chemistry.ReactionEffects;

/// <summary>
///     Explodes the body
/// </summary>
public sealed partial class Gib : EntityEffect
{
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("reagent-effect-guidebook-gib",
            ("chance", Probability)
        );

    public override void Effect(EntityEffectBaseArgs args)
    {
        var damageSystem = args.EntityManager.System<DamageableSystem>();
        var protoManager = IoCManager.Resolve<IPrototypeManager>();

        damageSystem.TryChangeDamage( // I could use the BodySystem, but for some reason the brain and organs don't fall out when it gibs.
            args.TargetEntity,
            new DamageSpecifier(protoManager.Index<DamageTypePrototype>("Blunt"), 10000),
            true
        );
    }
}
