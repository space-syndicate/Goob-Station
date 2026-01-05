using Content.Server.Effects;
using Content.Server.Imperial.Power.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Mobs.Components;
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
        SubscribeLocalEvent<SupermatterGasComponent, SupermatterGibAttemptEvent>(OnGibAttempt);
    }

    private void OnStartCollide(Entity<SupermatterTouchComponent> supermatter, ref StartCollideEvent args)
    {
        var other = args.OtherEntity;
        if (!EntityManager.HasComponent<MobStateComponent>(other))
            return;

        var gibEvent = new SupermatterGibAttemptEvent();
        RaiseLocalEvent(supermatter, ref gibEvent);
        if (gibEvent.Cancelled)
            return;

        var transformComp = Transform(other);

        Entity<TransformComponent> entity = new(other, transformComp);
        GibCollidedEntity(supermatter, entity);
        RaiseLocalEvent(supermatter, new SupermatterTouchedEvent());
    }

    private void OnGibAttempt(EntityUid uid, SupermatterGasComponent gasComp, ref SupermatterGibAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        var xform = Transform(uid);
        var gas = _atmosphereSystem.GetContainingMixture((uid, xform), true, true);
        if (gas == null)
            return;

        var hyperNobMoles = gas[(int) gasComp.HyperNobliumGas];
        if (hyperNobMoles > gasComp.GasActivationMoles)
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

public sealed class SupermatterTouchedEvent : EntityEventArgs;

[ByRefEvent]
public record struct SupermatterGibAttemptEvent(bool Cancelled = false);
