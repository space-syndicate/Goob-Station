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
            if (component.EffectEndTime.HasValue && currentTime >= component.EffectEndTime.Value)
            {
                DeactivateEffect(uid, component);
            }

            if (component.ActionEntity != null && TryComp<ActionComponent>(component.ActionEntity, out var action))
            {
                if (component.CooldownEndTime.HasValue)
                {
                    if (currentTime < component.CooldownEndTime.Value)
                    {
                        _actions.SetCooldown(component.ActionEntity, currentTime, component.CooldownEndTime.Value);
                    }
                    else if (action.Cooldown != null)
                    {
                        _actions.RemoveCooldown(component.ActionEntity);
                    }
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

        if (component.CooldownEndTime.HasValue && currentTime < component.CooldownEndTime.Value)
            return;

        args.Handled = true;
        ActivateEffect(entity.Owner, component, currentTime);
    }

    private void OnRefreshMovementSpeed(Entity<SandevistanComponent> entity, ref RefreshMovementSpeedModifiersEvent args)
    {
        var component = entity.Comp;
        var currentTime = _timing.CurTime;

        if (component.EffectEndTime.HasValue && currentTime < component.EffectEndTime.Value)
        {
            args.ModifySpeed(1f + component.SpeedModifierBonus, 1f + component.SpeedModifierBonus);
        }
    }

    private void ActivateEffect(EntityUid uid, SandevistanComponent component, TimeSpan currentTime)
    {
        var effectEndTime = currentTime + TimeSpan.FromSeconds(component.EffectDuration);
        var cooldownEndTime = effectEndTime + TimeSpan.FromSeconds(component.CooldownDuration);

        component.EffectEndTime = effectEndTime;
        component.CooldownEndTime = cooldownEndTime;
        Dirty(uid, component);

        EnsureComp<PhaseSpaceShadowComponent>(uid);

        EnsureComp<MovementSpeedModifierComponent>(uid);

        if (component.ActionEntity != null)
        {
            _actions.SetCooldown(component.ActionEntity, currentTime, cooldownEndTime);
        }

        _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);
    }

    private void DeactivateEffect(EntityUid uid, SandevistanComponent component)
    {
        component.EffectEndTime = null;
        Dirty(uid, component);

        RemComp<PhaseSpaceShadowComponent>(uid);

        _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);
    }
}

