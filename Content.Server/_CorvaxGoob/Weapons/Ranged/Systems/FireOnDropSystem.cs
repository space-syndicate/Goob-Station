using Content.Shared.Throwing;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Random;

namespace Content.Server._CorvaxGoob.Weapons.Ranged.Systems;

public sealed class FireOnDropSystem : EntitySystem
{
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GunComponent, ThrowDoHitEvent>(OnHit);
    }

    private void OnHit(Entity<GunComponent> ent, ref ThrowDoHitEvent args)
    {
        if (_random.Prob(ent.Comp.FireOnDropChance))
            _gun.AttemptShoot(ent, ent, Transform(ent).Coordinates.Offset(Transform(ent).LocalRotation.ToVec()));
    }
}
