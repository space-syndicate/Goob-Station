using JetBrains.Annotations;
using Robust.Shared.Timing;
using Content.Server.Explosion.EntitySystems;
using Content.Shared.Body.Systems;
using Content.Server.Imperial.ExplosiveProjectile.Components;

namespace Content.Server.Imperial.ExplosiveProjectile
{
    [UsedImplicitly]
    public sealed class ExplosiveProjectileResultOnSystem : EntitySystem
    {
        [Dependency] private readonly SharedBodySystem _body = default!;
        [Dependency] private readonly ExplosionSystem _explosion = default!;
        [Dependency] private readonly IGameTiming _timing = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ExplosiveProjectileResultOnComponent, ComponentStartup>(GetDelayTime);
        }
        private void GibEntity(EntityUid uid)
        {
            _explosion.QueueExplosion(uid, ExplosionSystem.DefaultExplosionPrototypeId, 1, 1, 1);
            _body.GibBody(uid, splatModifier: 5f);
            RemComp<ExplosiveProjectileResultOnComponent>(uid);
        }
        private void GetDelayTime(EntityUid uid, ExplosiveProjectileResultOnComponent component, ComponentStartup args)
        {
            component.DetonationTime = _timing.CurTime + component.DetonationTime;
        }
        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var query = EntityQueryEnumerator<ExplosiveProjectileResultOnComponent>();
            while (query.MoveNext(out var uid, out var comp))
            {
                if (_timing.CurTime >= comp.DetonationTime)
                    GibEntity(uid);
            }
        }
    }
}
