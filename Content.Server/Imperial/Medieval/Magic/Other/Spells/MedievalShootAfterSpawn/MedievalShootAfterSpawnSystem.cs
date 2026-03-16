using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.Imperial.CoordsHelper;
using Content.Shared.Imperial.Medieval.Magic;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Timing;

namespace Content.Server.Imperial.Medieval.Magic.MedievalShootAfterSpawn;


public sealed partial class MedievalShootAfterSpawnSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly GunSystem _gunSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MedievalShootAfterSpawnComponent, MedievalAfterAimingSpawnBySpellEvent>(OnSpawn);
    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var enumerator = EntityQueryEnumerator<MedievalShootAfterSpawnComponent, GunComponent>();

        while (enumerator.MoveNext(out var uid, out var component, out var gunComponent))
        {
            if (_timing.CurTime <= component.NextShot) continue;
            if (_timing.CurTime <= gunComponent.NextFire) continue;

            _gunSystem.AttemptShoot(
                uid,
                (uid, gunComponent),
                CoordsHelper.GetCoords(component.Target, EntityManager)
            );
        }
    }

    private void OnSpawn(EntityUid uid, MedievalShootAfterSpawnComponent component, MedievalAfterAimingSpawnBySpellEvent args)
    {
        component.Target = args.Target;
        component.Performer = args.Performer;
        component.NextShot = _timing.CurTime + component.ShootRate;
    }
}
