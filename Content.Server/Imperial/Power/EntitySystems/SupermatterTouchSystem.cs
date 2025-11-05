using Content.Server.Effects;
using Content.Server.Imperial.Power.Components;
using Content.Shared.Mobs.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;

namespace Content.Server.Imperial.Power.EntitySystems;

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

        var transformComp = Transform(other);

        GibCollidedEntity(component, transformComp, other, uid);
        RaiseLocalEvent(uid, new SupermatterTouchedEvent());
    }

    private void GibCollidedEntity(SupermatterTouchComponent supermatterTouchComponent, TransformComponent transformComp, EntityUid entityUid, EntityUid supermatterUid)
    {
        _audio.PlayPvs(supermatterTouchComponent.GibSound, transformComp.Coordinates);
        _colorFlash.RaiseEffect(supermatterTouchComponent.FlashColor, [supermatterUid], Filter.Pvs(supermatterUid));
        EntityManager.QueueDeleteEntity(entityUid);
        EntityManager.SpawnEntity(supermatterTouchComponent.AshPrototype, transformComp.Coordinates);

        if (TryComp<SupermatterIntegrityComponent>(supermatterUid, out var integrityComponent) && !integrityComponent.Activated)
            integrityComponent.Activated = true;
    }
}

public sealed class SupermatterTouchedEvent : EntityEventArgs;
