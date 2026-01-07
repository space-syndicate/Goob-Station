using Content.Server.Effects;
using Content.Server.Imperial.Power.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Mobs.Components;
using Content.Shared.Atmos;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;
using Robust.Shared.GameObjects;

namespace Content.Server.Imperial.Power.EntitySystems;

public sealed class SupermatterTouchSystem : EntitySystem
{
    [Dependency] private readonly ColorFlashEffectSystem _colorFlash = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SupermatterTouchComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<SupermatterGasComponent, SupermatterTouchedEvent>(OnTouched);
    }

    private void OnStartCollide(Entity<SupermatterTouchComponent> supermatter, ref StartCollideEvent args)
    {
        var other = args.OtherEntity;
        if (!EntityManager.HasComponent<MobStateComponent>(other))
            return;

        var touchEvent = new SupermatterTouchedEvent();
        RaiseLocalEvent(supermatter, ref touchEvent);
        if (touchEvent.Cancelled)
            return;

        var transformComp = Transform(other);

        Entity<TransformComponent> entity = new(other, transformComp);
        GibCollidedEntity(supermatter, entity);

    }

    private void OnTouched(Entity<SupermatterGasComponent> supermatter, ref SupermatterTouchedEvent args)
    {
        if (args.Cancelled)
            return;

        var xform = Transform(supermatter);
        var gas = _atmosphereSystem.GetContainingMixture((supermatter.Owner, xform), true, true);
        if (gas == null)
            return;

        var hyperNobMoles = gas.GetMoles(Gas.HyperNoblium);
        if (hyperNobMoles > supermatter.Comp.GasActivationMoles)
            args.Cancelled = true;
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

[ByRefEvent]
public record struct SupermatterTouchedEvent(bool Cancelled = false);
