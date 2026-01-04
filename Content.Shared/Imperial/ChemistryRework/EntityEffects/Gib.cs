using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared.Chemistry.ReactionEffects;


public sealed partial class GibEntityEffectSystem : EntityEffectSystem<DamageableComponent, Gib>
{
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;


    protected override void Effect(Entity<DamageableComponent> entity, ref EntityEffectEvent<Gib> args)
    {
        _damageableSystem.TryChangeDamage( // I could use the BodySystem, but for some reason the brain and organs don't fall out when it gibs.
            entity.Owner,
            new DamageSpecifier(_prototypeManager.Index<DamageTypePrototype>("Blunt"), 10000),
            true
        );
    }
}

/// <summary>
///     Explodes the body
/// </summary>
public sealed partial class Gib : EntityEffectBase<Gib>
{
    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("reagent-effect-guidebook-gib",
            ("chance", Probability)
        );
}
