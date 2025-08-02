using Content.Server.Imperial.Power.Components;
using Content.Shared.Mobs.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Events;
using Content.Server.Effects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Content.Shared.Effects;

namespace Content.Server.Imperial.Power.EntitySystems
{
    public sealed class SupermatterTouchSystem : EntitySystem
    {
        [Dependency] private readonly ColorFlashEffectSystem _colorFlash = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SupermatterTouchComponent, StartCollideEvent>(OnStartCollide);
        }

        private void OnStartCollide(EntityUid uid, SupermatterTouchComponent component, ref StartCollideEvent args)
        {
            var other = args.OtherEntity;
            if (!EntityManager.HasComponent<MobStateComponent>(other))
                return;

            var xform = EntityManager.GetComponentOrNull<TransformComponent>(other);
            if (xform == null)
                return;

            PlayGibSound(component, xform);
            ShowColorFlash(component, other);
            SpawnAshAndDelete(component, xform, other);
            RaiseLocalEvent(uid, new SupermatterTouchedEvent());
        }

        private void PlayGibSound(SupermatterTouchComponent component, TransformComponent xform)
        {
            _audio.PlayPvs(component.GibSound, xform.Coordinates);
        }

        private void ShowColorFlash(SupermatterTouchComponent component, EntityUid other)
        {
            _colorFlash.RaiseEffect(component.FlashColor, new List<EntityUid> { other }, Filter.Pvs(other));
        }

        private void SpawnAshAndDelete(SupermatterTouchComponent component, TransformComponent xform, EntityUid other)
        {
            EntityManager.SpawnEntity(component.AshPrototype, xform.Coordinates);
            EntityManager.QueueDeleteEntity(other);
        }
    }

    public sealed class SupermatterTouchedEvent : EntityEventArgs {}
}
