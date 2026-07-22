using Content.Server.Imperial.Power.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Effects;
using Content.Shared.Imperial.Power.Events;
using Content.Shared.Mobs.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared.Imperial.Power.Systems;

public sealed class SupermatterTouchSystem : EntitySystem
{
    [Dependency] private readonly SharedColorFlashEffectSystem _colorFlash = null!;
    [Dependency] private readonly SharedAudioSystem _audio = null!;
    [Dependency] private readonly IGameTiming _timing = null!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SupermatterTouchComponent, StartCollideEvent>(OnStartCollide);
    }

    private void OnStartCollide(Entity<SupermatterTouchComponent> ent, ref StartCollideEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;
        var other = args.OtherEntity;
        if (!HasComp<MobStateComponent>(other) || HasComp<GodmodeComponent>(other))
            return;

        var touchEvent = new SupermatterTouchedEvent();
        RaiseLocalEvent(ent, ref touchEvent);
        if (touchEvent.Cancelled)
            return;

        var transformComp = Transform(other);
        GibCollidedEntity(ent, (other, transformComp));
    }

    private void GibCollidedEntity(Entity<SupermatterTouchComponent> supermatter, Entity<TransformComponent> entity)
    {
        _audio.PlayPvs(supermatter.Comp.GibSound, entity.Comp.Coordinates);
        _colorFlash.RaiseEffect(supermatter.Comp.FlashColor, new List<EntityUid>() { supermatter }, Filter.Pvs(supermatter));
        Spawn(supermatter.Comp.AshPrototype, entity.Comp.Coordinates);
        QueueDel(entity);
    }
}
