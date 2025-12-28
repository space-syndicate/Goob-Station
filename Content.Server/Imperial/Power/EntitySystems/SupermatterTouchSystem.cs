using Content.Server.Effects;
using Content.Server.Imperial.Power.Components;
using Content.Shared.Mobs.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;

namespace Content.Server.Imperial.Power.EntitySystems;

public sealed class SupermatterTouchSystem : EntitySystem
{
    [Dependency] private readonly ColorFlashEffectSystem _colorFlash = null!;
    [Dependency] private readonly SharedAudioSystem _audio = null!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SupermatterTouchComponent, StartCollideEvent>(OnStartCollide);
    }

    private void OnStartCollide(Entity<SupermatterTouchComponent> supermatter, ref StartCollideEvent args)
    {
        var other = args.OtherEntity;
        if (!EntityManager.HasComponent<MobStateComponent>(other))
            return;

        var transformComp = Transform(other);

        Entity<TransformComponent> entity = new(other, transformComp);
        GibCollidedEntity(supermatter, entity);
        RaiseLocalEvent(supermatter, new SupermatterTouchedEvent());
    }

    private void GibCollidedEntity(Entity<SupermatterTouchComponent> supermatter, Entity<TransformComponent> entity)
    {
        _audio.PlayPvs(supermatter.Comp.GibSound, entity.Comp.Coordinates);
        _colorFlash.RaiseEffect(supermatter.Comp.FlashColor, [supermatter], Filter.Pvs(supermatter));
        EntityManager.QueueDeleteEntity(entity);
        EntityManager.SpawnEntity(supermatter.Comp.AshPrototype, entity.Comp.Coordinates);

        if (TryComp<SupermatterIntegrityComponent>(supermatter, out var integrityComponent) && !integrityComponent.Activated)
            integrityComponent.Activated = true;
    }
}

public sealed class SupermatterTouchedEvent : EntityEventArgs;
