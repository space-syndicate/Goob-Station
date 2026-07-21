using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Imperial.DeimonFly.RussianRoulette;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Traits.Assorted;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Random;

namespace Content.Server.Imperial.DeimonFly.RussianRoulette;

/// <summary>
/// Применяет эффект револьвера для русской рулетки к сущности, совершившей выстрел.
/// </summary>
public sealed class RussianRouletteGunSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedContainerSystem _containers = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RussianRouletteGunComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<RussianRouletteGunComponent, GunShotEvent>(OnGunShot);
    }

    private void OnMapInit(Entity<RussianRouletteGunComponent> gun, ref MapInitEvent args)
    {
        if (!TryComp<RevolverAmmoProviderComponent>(gun, out var revolver) || revolver.Capacity <= 0)
            return;

        // Клиент и сервер создают унаследованный револьвер пустым. Здесь сервер помещает
        // единственный настоящий патрон в случайную камору и синхронизирует барабан целиком.
        Array.Fill(revolver.Chambers, null);
        var chamber = _random.Next(revolver.Capacity);
        var cartridge = Spawn(gun.Comp.Cartridge, Transform(gun).Coordinates);

        revolver.AmmoSlots[chamber] = cartridge;
        revolver.Chambers[chamber] = true;
        _containers.Insert(cartridge, revolver.AmmoContainer);
        Dirty(gun.Owner, revolver);
    }

    private void OnGunShot(Entity<RussianRouletteGunComponent> gun, ref GunShotEvent args)
    {
        // GunShotEvent вызывается только после подтверждения выстрела и расходования боеприпаса.
        if (Deleted(args.User))
            return;

        // Сервер отменяет полёт внешнего снаряда, поэтому сам воспроизводит звук для остальных игроков
        // и удаляет уже созданную штатной системой безвредную пулю.
        if (TryComp<GunComponent>(gun, out var gunComponent))
            _audio.PlayPredicted(gunComponent.SoundGunshotModified, gun, args.User);

        foreach (var (projectile, _) in args.Ammo)
        {
            if (projectile is { } projectileUid)
                QueueDel(projectileUid);
        }

        // Добавляем компонент до смертельного урона, чтобы системы воскрешения увидели его на трупе.
        // Если выстрел не убил сущность, временную метку ниже обязательно снимаем.
        var addedUnrevivable = gun.Comp.PreventRevival && !HasComp<UnrevivableComponent>(args.User);
        if (addedUnrevivable)
            EnsureComp<UnrevivableComponent>(args.User);

        // Копируем значение из прототипа, поскольку события урона могут изменять DamageSpecifier.
        var damage = new DamageSpecifier(gun.Comp.Damage);
        _damageable.TryChangeDamage(
            args.User,
            damage,
            ignoreResistances: true,
            origin: gun.Owner);

        if (addedUnrevivable &&
            !Deleted(args.User) &&
            (!TryComp<MobStateComponent>(args.User, out var mobState) || !_mobState.IsDead(args.User, mobState)))
        {
            RemComp<UnrevivableComponent>(args.User);
        }
    }
}
