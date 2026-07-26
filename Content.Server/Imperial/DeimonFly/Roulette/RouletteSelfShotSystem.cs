using Content.Shared.Imperial.DeimonFly.BuckshotRoulette;
using Content.Shared.Imperial.DeimonFly.RussianRoulette;
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Server.Imperial.DeimonFly.Roulette;

/// <summary>
/// Отменяет создание внешнего снаряда для оружия, которое должно выстрелить в своего пользователя.
/// Последствия подтверждённого выстрела обрабатывают системы конкретного оружия.
/// </summary>
public sealed class RouletteSelfShotSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        // MetaDataComponent присутствует у любой сущности, поэтому правило действует
        // даже на нестандартного стрелка без MobStateComponent или DamageableComponent.
        SubscribeLocalEvent<MetaDataComponent, SelfBeforeGunShotEvent>(OnBeforeGunShot);
    }

    private void OnBeforeGunShot(Entity<MetaDataComponent> shooter, ref SelfBeforeGunShotEvent args)
    {
        if (HasComp<RussianRouletteGunComponent>(args.Gun) ||
            TryComp<BuckshotRouletteShotgunComponent>(args.Gun, out var shotgun) &&
            shotgun.FireMode == BuckshotRouletteFireMode.Self)
        {
            args.Cancel();
        }
    }
}
