using Content.Shared.Weapons.Melee.Events;

namespace Content.Shared._CorvaxGoob.Weapons.Melee.SwipeCombat;

/// <summary>
/// У свайп-оружия нет обычных лёгких и тяжёлых атак — только свайпы.
/// Свайп-атаки идут мимо <see cref="AttemptMeleeEvent"/>, поэтому их это не затрагивает.
/// </summary>
public sealed class SwipeCombatBlockSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SwipeCombatComponent, AttemptMeleeEvent>(OnAttemptMelee);
    }

    private void OnAttemptMelee(Entity<SwipeCombatComponent> ent, ref AttemptMeleeEvent args)
    {
        args.Cancelled = true;
    }
}
