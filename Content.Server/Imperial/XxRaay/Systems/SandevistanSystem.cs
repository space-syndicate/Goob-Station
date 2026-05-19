using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Actions.Events;
using Content.Shared.Imperial.PhaseSpace;
using Content.Shared.Imperial.XxRaay.Components;
using Content.Shared.Imperial.XxRaay.Events;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Shared.Timing;

namespace Content.Server.Imperial.XxRaay.Systems;

/// <summary>
/// Система для управления эффектом сандевистана.
/// </summary>
public sealed class SandevistanSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SandevistanComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<SandevistanComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<SandevistanComponent, SandevistanActionEvent>(OnSandevistanActivated);
        SubscribeLocalEvent<SandevistanComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeed);
    }

    private void OnComponentInit(Entity<SandevistanComponent> entity, ref ComponentInit args)
    {
        _actions.AddAction(entity.Owner, ref entity.Comp.ActionEntity, entity.Comp.ActionPrototype, entity.Owner);
    }

    private void OnComponentShutdown(Entity<SandevistanComponent> entity, ref ComponentShutdown args)
    {
        if (entity.Comp.ActionEntity != null)
        {
            _actions.RemoveAction(entity.Comp.ActionEntity);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var currentTime = _timing.CurTime;
        var query = EntityQueryEnumerator<SandevistanComponent>();

        while (query.MoveNext(out var uid, out var component))
        {
            if (component.EffectEndTime != TimeSpan.Zero && currentTime >= component.EffectEndTime)
            {
                DeactivateEffect(uid, component);
            }

            if (component.ActionEntity != null && TryComp<ActionComponent>(component.ActionEntity, out var action))
            {
                if (currentTime < component.CooldownEndTime)
                {
                    if (action.Cooldown == null || 
                        action.Cooldown.Value.Start != component.CooldownStartTime ||
                        action.Cooldown.Value.End != component.CooldownEndTime)
                    {
                        _actions.SetCooldown(component.ActionEntity, component.CooldownStartTime, component.CooldownEndTime);
                    }
                }
                else if (action.Cooldown != null)
                {
                    _actions.RemoveCooldown(component.ActionEntity);
                }
            }
        }
    }

    private void OnSandevistanActivated(Entity<SandevistanComponent> entity, ref SandevistanActionEvent args)
    {
        if (args.Handled)
            return;

        var component = entity.Comp;
        var currentTime = _timing.CurTime;

        var effectEnded = component.EffectEndTime == TimeSpan.Zero || currentTime >= component.EffectEndTime;
        if (effectEnded && currentTime < component.CooldownEndTime)
            return;

        args.Handled = true;
        ActivateEffect(entity.Owner, component);
    }

    private void OnRefreshMovementSpeed(Entity<SandevistanComponent> entity, ref RefreshMovementSpeedModifiersEvent args)
    {
        var component = entity.Comp;
        var currentTime = _timing.CurTime;

        if (component.EffectEndTime != TimeSpan.Zero && currentTime < component.EffectEndTime)
        {
            args.ModifySpeed(1f + component.SpeedModifierBonus, 1f + component.SpeedModifierBonus);
        }
    }

    private void ActivateEffect(EntityUid uid, SandevistanComponent component)
    {
        var currentTime = _timing.CurTime;
        var effectEndTime = currentTime + component.EffectDuration;
        var cooldownStartTime = currentTime;
        var cooldownEndTime = effectEndTime + component.CooldownDuration;

        component.EffectEndTime = effectEndTime;
        component.CooldownStartTime = cooldownStartTime;
        component.CooldownEndTime = cooldownEndTime;
        Dirty(uid, component);

        EnsureComp<PhaseSpaceShadowComponent>(uid);

        EnsureComp<MovementSpeedModifierComponent>(uid);

        if (component.ActionEntity != null)
        {
            _actions.SetCooldown(component.ActionEntity, cooldownStartTime, cooldownEndTime);
        }

        _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);
    }

    private void DeactivateEffect(EntityUid uid, SandevistanComponent component)
    {
        component.EffectEndTime = TimeSpan.Zero;
        Dirty(uid, component);

        RemComp<PhaseSpaceShadowComponent>(uid);

        _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);
    }
}

