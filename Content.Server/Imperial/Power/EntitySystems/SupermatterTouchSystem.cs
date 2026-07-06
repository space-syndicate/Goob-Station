using Content.Server.Effects;
using Content.Server.Imperial.Power.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Imperial.Power.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;
using Content.Server.Imperial.Power.EntitySystems.Events;
using Content.Shared.Damage.Components;

namespace Content.Server.Imperial.Power.EntitySystems;

public sealed class SupermatterTouchSystem : EntitySystem
{
    [Dependency] private readonly ColorFlashEffectSystem _colorFlash = null!;
    [Dependency] private readonly SharedAudioSystem _audio = null!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SupermatterTouchComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<SupermatterGasComponent, SupermatterTouchedEvent>(OnTouched, before: [typeof(SupermatterEventSystem)]);
    }

    private void OnStartCollide(Entity<SupermatterTouchComponent> supermatter, ref StartCollideEvent args)
    {
        var other = args.OtherEntity;
        if (!HasComp<MobStateComponent>(other) || HasComp<GodmodeComponent>(other))
            return;

        var touchEvent = new SupermatterTouchedEvent();
        RaiseLocalEvent(supermatter, ref touchEvent);
        if (touchEvent.Cancelled)
            return;

        var transformComp = Transform(other);
        GibCollidedEntity(supermatter, (other, transformComp));

    }

    private static void OnTouched(Entity<SupermatterGasComponent> supermatter, ref SupermatterTouchedEvent args)
    {
        if (args.Cancelled)
            return;

        if (supermatter.Comp.RuntimeDisableTouchGib)
            args.Cancelled = true;
    }

    private void GibCollidedEntity(Entity<SupermatterTouchComponent> supermatter, Entity<TransformComponent> entity)
    {
        _audio.PlayPvs(supermatter.Comp.GibSound, entity.Comp.Coordinates);
        _colorFlash.RaiseEffect(supermatter.Comp.FlashColor, [supermatter], Filter.Pvs(supermatter));
        QueueDel(entity);
        Spawn(supermatter.Comp.AshPrototype, entity.Comp.Coordinates);

        if (TryComp<SupermatterIntegrityComponent>(supermatter, out var integrityComponent) && !integrityComponent.Activated)
            integrityComponent.Activated = true;
    }
}
