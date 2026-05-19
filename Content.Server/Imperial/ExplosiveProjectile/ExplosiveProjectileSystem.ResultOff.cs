using JetBrains.Annotations;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Timing;
using Content.Shared.Audio;
using Content.Server.Imperial.ExplosiveProjectile.Components;

namespace Content.Server.Imperial.ExplosiveProjectile
{
    [UsedImplicitly]
    public sealed class ExplosiveProjectileResultOffSystem : EntitySystem
    {
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly IGameTiming _timing = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ExplosiveProjectileResultOffComponent, ComponentStartup>(GetDelayTime);
        }
        private void DoNotGibEntity(EntityUid uid, ExplosiveProjectileResultOffComponent component)
        {
            _audio.PlayPvs(component.SoundDeactivate, uid);
            RemComp<ExplosiveProjectileResultOffComponent>(uid);
        }
        private void GetDelayTime(EntityUid uid, ExplosiveProjectileResultOffComponent component, ComponentStartup args)
        {
            component.CancelTime = _timing.CurTime + component.CancelTime;
        }
        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var query = EntityQueryEnumerator<ExplosiveProjectileResultOffComponent>();
            while (query.MoveNext(out var uid, out var comp))
            {
                if (_timing.CurTime >= comp.CancelTime)
                    DoNotGibEntity(uid, comp);
            }
        }
    }
}
