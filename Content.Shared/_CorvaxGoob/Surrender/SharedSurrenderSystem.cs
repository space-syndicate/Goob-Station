using Content.Goobstation.Common.Cuffs;
using Content.Shared.Alert;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Pointing;
using Content.Shared.Pulling.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Shared._CorvaxGoob.Surrender;

public sealed partial class SharedSurrenderSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SurrenderComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeed);
        SubscribeLocalEvent<SurrenderComponent, AccessibleOverrideEvent>(OnAccessibleOverride);
        SubscribeLocalEvent<SurrenderComponent, AttackAttemptEvent>(OnAttackAttempt);
        SubscribeLocalEvent<SurrenderComponent, InteractionAttemptEvent>(OnInteractionAttempt);
        SubscribeLocalEvent<SurrenderComponent, ModifyBeingCuffDurationEvent>(OnModifyCuffDuration);
        SubscribeLocalEvent<SurrenderComponent, AttemptStopPullingEvent>(OnAttemptStopPulling);

    }

    private void OnAttemptStopPulling(Entity<SurrenderComponent> entity, ref AttemptStopPullingEvent ev)
    {
        if (entity != ev.User)
            return;

        ev.Cancelled = true;
    }

    private void OnModifyCuffDuration(Entity<SurrenderComponent> entity, ref ModifyBeingCuffDurationEvent ev)
    {
        ev.Duration *= entity.Comp.InteractionWithUserMod;
    }

    private void OnInteractionAttempt(Entity<SurrenderComponent> entity, ref InteractionAttemptEvent ev)
    {
        ev.Cancelled = true;
    }

    private void OnAttackAttempt(Entity<SurrenderComponent> entity, ref AttackAttemptEvent ev)
    {
        ev.Cancel();
    }

    private void OnAccessibleOverride(Entity<SurrenderComponent> entity, ref AccessibleOverrideEvent ev)
    {
        if (ev.User != entity.Owner)
            return;

        ev.Accessible = false;
        ev.Handled = true;
    }

    private void OnRefreshMovementSpeed(Entity<SurrenderComponent> entity, ref RefreshMovementSpeedModifiersEvent ev)
    {
        ev.ModifySpeed(entity.Comp.MovementSpeedMod);
    }

}
