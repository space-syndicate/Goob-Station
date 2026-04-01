// SPDX-FileCopyrightText: 2026 OpenAI
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Goobstation.Common.MartialArts;
using Content.Goobstation.Shared.MartialArts.Components;
using Content.Goobstation.Shared.Weapons.Parry.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Hypospray.Events;
using Content.Shared.CombatMode;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.Toggleable;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Reflect;
using Content.Shared.Wieldable;
using Content.Shared.Wieldable.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Goobstation.Shared.Weapons.Parry;

public sealed class ParrySystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedCombatModeSystem _combatMode = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedMeleeWeaponSystem _melee = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ReflectSystem _reflect = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ThrownItemSystem _thrown = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ParryComponent, MapInitEvent>(OnParryMapInit);
        SubscribeLocalEvent<ParryComponent, ComponentShutdown>(OnParryShutdown);
        SubscribeLocalEvent<ParryComponent, GetItemActionsEvent>(OnGetItemActions);
        SubscribeLocalEvent<ParryComponent, ToggleActionEvent>(OnToggleAction);
        SubscribeLocalEvent<ParryComponent, GotUnequippedHandEvent>(OnUnequippedHand);
        SubscribeLocalEvent<ParryComponent, DroppedEvent>(OnDropped);
        SubscribeLocalEvent<ParryComponent, DidEquipHandEvent>(OnDidEquipHand);
        SubscribeLocalEvent<ParryComponent, ItemUnwieldedEvent>(OnItemUnwielded);

        SubscribeLocalEvent<MeleeWeaponComponent, ComponentInit>(OnMeleeInit);
        SubscribeLocalEvent<HyposprayComponent, ComponentInit>(OnHyposprayInit);

        SubscribeLocalEvent<CqcParryComponent, ComponentInit>(OnCqcInit);
        SubscribeLocalEvent<SleepingCarpParryComponent, ComponentInit>(OnSleepingCarpInit);

        SubscribeLocalEvent<MetaDataComponent, GettingAttackedAttemptEvent>(OnGettingAttacked);
        SubscribeLocalEvent<MetaDataComponent, TargetBeforeHyposprayInjectsEvent>(OnTargetBeforeHyposprayInjects);
        SubscribeLocalEvent<MetaDataComponent, ThrowHitByEvent>(OnThrowHitBy);
        SubscribeLocalEvent<MetaDataComponent, ProjectileReflectAttemptEvent>(OnProjectileReflectAttempt);
        SubscribeLocalEvent<ParryAttackBlockComponent, AttackAttemptEvent>(OnAttackAttempt);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CqcParryComponent, ParryComponent>();
        while (query.MoveNext(out var uid, out _, out var parry))
        {
            if (!parry.Active || parry.AlwaysActive)
                continue;

            if (CanActivateCqcParry(uid))
                continue;

            SetParryActive((uid, parry), false);
        }
    }

    private void OnParryMapInit(Entity<ParryComponent> ent, ref MapInitEvent args)
    {
        RefreshAction(ent);
        UpdateActionToggle(ent);
    }

    private void OnParryShutdown(Entity<ParryComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.Active && !ent.Comp.AlwaysActive)
            SetParryActive(ent, false);

        if (ent.Comp.ActionEntity != null)
            _actions.RemoveAction(ent.Comp.ActionEntity);
    }

    private void OnGetItemActions(Entity<ParryComponent> ent, ref GetItemActionsEvent args)
    {
        if (ent.Comp.Action == null)
            return;

        args.AddAction(ref ent.Comp.ActionEntity, ent.Comp.Action);
    }

    private void OnToggleAction(Entity<ParryComponent> ent, ref ToggleActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        SetParryActive(ent, !ent.Comp.Active, args.Performer);
    }

    private void OnUnequippedHand(Entity<ParryComponent> ent, ref GotUnequippedHandEvent args)
    {
        if (ent.Comp.Active)
            SetParryActive(ent, false);
    }

    private void OnDropped(Entity<ParryComponent> ent, ref DroppedEvent args)
    {
        if (ent.Comp.Active)
            SetParryActive(ent, false);
    }

    private void OnDidEquipHand(Entity<ParryComponent> ent, ref DidEquipHandEvent args)
    {
        if (!ent.Comp.Active || ent.Comp.RequireFreeHands <= 0 || ent.Owner != args.User)
            return;

        SetParryActive(ent, false);
    }

    private void OnItemUnwielded(Entity<ParryComponent> ent, ref ItemUnwieldedEvent args)
    {
        if (ent.Comp.Active)
            SetParryActive(ent, false);
    }

    private void OnMeleeInit(Entity<MeleeWeaponComponent> ent, ref ComponentInit args)
    {
        var parriable = EnsureComp<ParriableComponent>(ent);
        parriable.ParryableAsMelee = true;
    }

    private void OnHyposprayInit(Entity<HyposprayComponent> ent, ref ComponentInit args)
    {
        var parriable = EnsureComp<ParriableComponent>(ent);
        parriable.ParryableAsInjectable = true;
    }

    private void OnCqcInit(Entity<CqcParryComponent> ent, ref ComponentInit args)
    {
        var parry = EnsureComp<ParryComponent>(ent);
        parry.Action = "ActionToggleCQCParry";
        parry.CanParryMelee = true;
        parry.CanParryInjectables = false;
        parry.CanParryThrown = false;
        parry.CanParryProjectiles = false;
        parry.ParryChance = 0.75f;
        parry.Cooldown = TimeSpan.FromSeconds(2);
        parry.CounterAttack = true;
        parry.CounterAttackStunTime = TimeSpan.FromSeconds(4);
        parry.CounterAttackKnockdownTime = TimeSpan.Zero;
        parry.AttackerBlockTime = TimeSpan.FromSeconds(2);
        parry.BlockCombosWhileActive = true;
        parry.DisableMartialArtsWhileActive = true;
        parry.AlwaysActive = false;
        parry.RequireFreeHands = 2;
        parry.RequireStanding = true;
        parry.SelfPopup = "parry-popup-self-cqc";
        parry.AttackerPopup = "parry-popup-attacker-cqc";
        RefreshAction((ent.Owner, parry));
        UpdateActionToggle((ent.Owner, parry));
    }

    private void OnSleepingCarpInit(Entity<SleepingCarpParryComponent> ent, ref ComponentInit args)
    {
        var parry = EnsureComp<ParryComponent>(ent);
        parry.Action = null;
        parry.CanParryMelee = false;
        parry.CanParryInjectables = true;
        parry.CanParryThrown = true;
        parry.CanParryProjectiles = false;
        parry.ParryChance = 1f;
        parry.Cooldown = TimeSpan.Zero;
        parry.CounterAttack = false;
        parry.AlwaysActive = true;
        parry.Active = true;
        parry.ReflectThrown = true;
        parry.RequireStanding = true;
        UpdateActionToggle((ent.Owner, parry));
    }

    private void OnGettingAttacked(EntityUid uid, MetaDataComponent component, ref GettingAttackedAttemptEvent args)
    {
        if (_net.IsClient || args.Cancelled)
            return;

        if (!TryGetParry(uid, ParryChannel.Melee, out var source, out var parry))
            return;

        var attackSource = args.Weapon ?? args.Attacker;
        if (ShouldIgnoreParry(uid, args.Attacker, attackSource, source) ||
            !CanBeParried(attackSource, ParryChannel.Melee, out var bypasses) ||
            bypasses)
            return;

        if (!TryUseParry((source, parry), uid, args.Attacker, ParryChannel.Melee))
            return;

        args.Cancelled = true;
    }

    private void OnTargetBeforeHyposprayInjects(EntityUid uid, MetaDataComponent component, ref TargetBeforeHyposprayInjectsEvent args)
    {
        if (_net.IsClient || args.Cancelled)
            return;

        if (!TryGetParry(uid, ParryChannel.Injectable, out var source, out var parry))
            return;

        if (ShouldIgnoreParry(uid, args.EntityUsingHypospray, args.Hypospray, source) ||
            !CanBeParried(args.Hypospray, ParryChannel.Injectable, out var bypasses) ||
            bypasses)
            return;

        if (!TryUseParry((source, parry), uid, args.EntityUsingHypospray, ParryChannel.Injectable))
            return;

        args.InjectMessageOverride = "parry-popup-hypospray-blocked";
        args.Cancel();
    }

    private void OnThrowHitBy(EntityUid uid, MetaDataComponent component, ref ThrowHitByEvent args)
    {
        if (_net.IsClient || args.Handled)
            return;

        if (!TryGetParry(uid, ParryChannel.Thrown, out var source, out var parry))
            return;

        if (ShouldIgnoreParry(uid, args.Component.Thrower, args.Thrown, source) ||
            !CanBeParried(args.Thrown, ParryChannel.Thrown, out var bypasses) ||
            bypasses)
            return;

        if (!TryUseParry((source, parry), uid, args.Component.Thrower, ParryChannel.Thrown))
            return;

        args.Handled = true;
        HandleThrownParry(uid, args.Thrown, args.Component, parry);
    }

    private void OnProjectileReflectAttempt(EntityUid uid, MetaDataComponent component, ref ProjectileReflectAttemptEvent args)
    {
        if (_net.IsClient || args.Cancelled)
            return;

        if (!TryGetParry(uid, ParryChannel.Projectile, out var source, out var parry))
            return;

        if (!TryComp<ReflectComponent>(source, out var reflect))
            return;

        if (!TryUseParry((source, parry), uid, args.Component.Shooter, ParryChannel.Projectile, false))
            return;

        if (!_reflect.TryReflectProjectile((source, reflect), uid, (args.ProjUid, args.Component)))
            return;

        args.Cancelled = true;
        ConsumeParry((source, parry), uid, args.Component.Shooter, ParryChannel.Projectile, false);
    }

    private void OnAttackAttempt(EntityUid uid, ParryAttackBlockComponent component, ref AttackAttemptEvent args)
    {
        if (args.Target == null)
            return;

        CleanParryAttackBlocks(uid, component);

        if (!component.BlockedTargets.ContainsKey(args.Target.Value))
            return;

        args.Cancel();
    }

    private bool TryGetParry(EntityUid defender, ParryChannel channel, out EntityUid source, out ParryComponent parry)
    {
        if (TryComp(defender, out ParryComponent? selfParry) && CanUseParry(defender, defender, selfParry, channel))
        {
            parry = selfParry;
            source = defender;
            return true;
        }

        if (TryComp(defender, out HandsComponent? hands))
        {
            foreach (var held in _hands.EnumerateHeld((defender, hands)))
            {
                if (!TryComp(held, out ParryComponent? heldParry) || !CanUseParry(held, defender, heldParry, channel))
                    continue;

                parry = heldParry;
                source = held;
                return true;
            }
        }

        source = EntityUid.Invalid;
        parry = default!;
        return false;
    }

    private bool CanUseParry(EntityUid source, EntityUid defender, ParryComponent parry, ParryChannel channel)
    {
        if (!parry.AlwaysActive && !parry.Active)
            return false;

        if (parry.NextParry > _timing.CurTime)
            return false;

        if (!_actionBlocker.CanInteract(defender, null))
            return false;

        if (parry.RequireStanding && _standing.IsDown(defender))
            return false;

        if (parry.RequireWielded &&
            (!TryComp<WieldableComponent>(source, out var wieldable) || !wieldable.Wielded))
        {
            return false;
        }

        if (parry.RequireFreeHands > 0 &&
            _hands.GetEmptyHandCount(defender) < parry.RequireFreeHands)
        {
            return false;
        }

        return channel switch
        {
            ParryChannel.Melee => parry.CanParryMelee,
            ParryChannel.Injectable => parry.CanParryInjectables,
            ParryChannel.Thrown => parry.CanParryThrown,
            ParryChannel.Projectile => parry.CanParryProjectiles,
            _ => false,
        };
    }

    private bool CanBeParried(EntityUid source, ParryChannel channel, out bool bypasses)
    {
        bypasses = false;

        if (TryComp<ParriableComponent>(source, out var parriable))
        {
            var canBeParried = channel switch
            {
                ParryChannel.Melee => parriable.ParryableAsMelee,
                ParryChannel.Injectable => parriable.ParryableAsInjectable,
                ParryChannel.Thrown => parriable.ParryableAsThrown,
                _ => false,
            };

            bypasses = canBeParried &&
                (parriable.BypassesParry ||
                 parriable.ParryBypassChance >= 1f ||
                 (parriable.ParryBypassChance > 0f && _random.Prob(parriable.ParryBypassChance)));
            return canBeParried;
        }

        return channel switch
        {
            ParryChannel.Melee => HasComp<MeleeWeaponComponent>(source),
            ParryChannel.Injectable => HasComp<HyposprayComponent>(source),
            ParryChannel.Thrown => HasComp<Content.Shared.Ensnaring.Components.EnsnaringComponent>(source),
            _ => false,
        };
    }

    private bool TryUseParry(
        Entity<ParryComponent> parry,
        EntityUid defender,
        EntityUid? attacker,
        ParryChannel channel,
        bool consume = true)
    {
        if (parry.Comp.ParryChance < 1f && !_random.Prob(parry.Comp.ParryChance))
            return false;

        if (consume)
            ConsumeParry(parry, defender, attacker, channel, true);

        return true;
    }

    private bool ShouldIgnoreParry(EntityUid defender, EntityUid? attacker, EntityUid attackSource, EntityUid parrySource)
    {
        if (attacker == null)
            return false;

        return attacker == defender ||
               attackSource == defender ||
               attackSource == parrySource;
    }

    private void ConsumeParry(
        Entity<ParryComponent> parry,
        EntityUid defender,
        EntityUid? attacker,
        ParryChannel channel,
        bool canCounter)
    {
        parry.Comp.NextParry = _timing.CurTime + parry.Comp.Cooldown;

        if (parry.Comp.ParrySound != null)
            _audio.PlayPvs(parry.Comp.ParrySound, defender);

        if (parry.Comp.SelfPopup != null)
            _popup.PopupEntity(Loc.GetString(parry.Comp.SelfPopup), defender, defender, PopupType.MediumCaution);

        if (attacker != null && !TerminatingOrDeleted(attacker.Value) && parry.Comp.AttackerPopup != null)
            _popup.PopupEntity(Loc.GetString(parry.Comp.AttackerPopup), attacker.Value, attacker.Value, PopupType.MediumCaution);

        if (attacker != null &&
            !TerminatingOrDeleted(attacker.Value) &&
            parry.Comp.AttackerBlockTime > TimeSpan.Zero)
        {
            AddAttackBlock(attacker.Value, defender, parry.Comp.AttackerBlockTime);
        }

        if (!canCounter || !parry.Comp.CounterAttack || attacker == null || channel != ParryChannel.Melee)
            return;

        CounterAttack(defender, attacker.Value, parry.Comp);
    }

    private void CounterAttack(EntityUid defender, EntityUid attacker, ParryComponent parry)
    {
        if (!TryComp(defender, out MeleeWeaponComponent? melee))
            return;

        if (!_actionBlocker.CanAttack(defender, attacker, (defender, melee)))
            return;

        var nextAttack = melee.NextAttack;
        melee.NextAttack = TimeSpan.Zero;

        RaiseLocalEvent(defender, new SaveLastAttacksEvent());
        RaiseLocalEvent(defender, new ResetLastAttacksEvent(false));

        var inCombat = _combatMode.IsInCombatMode(defender);
        if (!inCombat)
            _combatMode.SetInCombatMode(defender, true);

        if (_melee.AttemptLightAttack(defender, defender, melee, attacker))
        {
            if (parry.CounterAttackStunTime > TimeSpan.Zero)
                _stun.TryUpdateStunDuration(attacker, parry.CounterAttackStunTime);

            if (parry.CounterAttackKnockdownTime > TimeSpan.Zero)
                _stun.TryKnockdown(attacker, parry.CounterAttackKnockdownTime, true);
        }

        if (!inCombat)
            _combatMode.SetInCombatMode(defender, false);

        RaiseLocalEvent(defender, new LoadLastAttacksEvent());

        melee.NextAttack = nextAttack;
        Dirty(defender, melee);
    }

    private void HandleThrownParry(EntityUid defender, EntityUid thrown, ThrownItemComponent component, ParryComponent parry)
    {
        if (parry.ReflectThrown)
        {
            ReflectThrown(defender, thrown, component);
            return;
        }

        _thrown.StopThrow(thrown, component);
    }

    private void ReflectThrown(EntityUid defender, EntityUid thrown, ThrownItemComponent component)
    {
        _thrown.StopThrow(thrown, component);

        if (component.Thrower == null || TerminatingOrDeleted(component.Thrower.Value))
            return;

        var defenderPos = _transform.GetMapCoordinates(defender).Position;
        var throwerPos = _transform.GetMapCoordinates(component.Thrower.Value).Position;
        var direction = throwerPos - defenderPos;

        if (direction.LengthSquared() <= 0.001f)
            return;

        var speed = 10f;
        if (TryComp<PhysicsComponent>(thrown, out var physics))
            speed = MathF.Max(speed, _physics.GetMapLinearVelocity(thrown, component: physics).Length());

        _throwing.TryThrow(thrown, direction, speed, defender, recoil: false, playSound: false);
    }

    private void SetParryActive(Entity<ParryComponent> ent, bool active, EntityUid? user = null)
    {
        var actualUser = user ?? ent.Owner;

        if (active &&
            TryComp<CqcParryComponent>(ent, out _) &&
            !CanActivateCqcParry(actualUser))
        {
            return;
        }

        if (active &&
            ent.Comp.RequireWielded &&
            (!TryComp<WieldableComponent>(ent, out var wieldable) || !wieldable.Wielded))
        {
            if (user != null)
                _popup.PopupEntity(Loc.GetString("parry-popup-requires-wielded"), user.Value, user.Value, PopupType.MediumCaution);
            return;
        }

        if (active &&
            ent.Comp.RequireFreeHands > 0 &&
            _hands.GetEmptyHandCount(actualUser) < ent.Comp.RequireFreeHands)
        {
            _popup.PopupEntity(
                Loc.GetString("parry-popup-requires-free-hands", ("count", ent.Comp.RequireFreeHands)),
                actualUser,
                actualUser,
                PopupType.MediumCaution);
            return;
        }

        ent.Comp.Active = active || ent.Comp.AlwaysActive;
        UpdateCqcSuppression(ent, actualUser);
        UpdateActionToggle(ent);
    }

    private bool CanActivateCqcParry(EntityUid user)
    {
        if (!TryComp<MartialArtsKnowledgeComponent>(user, out var knowledge) ||
            knowledge.MartialArtsForm != MartialArtsForms.CloseQuartersCombat)
        {
            return false;
        }

        var originalBlocked = knowledge.Blocked;
        if (TryComp<CqcParryComponent>(user, out var cqc) && cqc.RememberedBlockedState)
            knowledge.Blocked = cqc.HadBlockedState;

        var wasActive = false;

        if (TryComp<ParryComponent>(user, out ParryComponent? parry))
        {
            wasActive = parry.Active;
            if (wasActive)
                parry.Active = false;

            var ev = new CanDoCQCEvent();
            RaiseLocalEvent(user, ev);

            if (wasActive)
                parry.Active = true;

            knowledge.Blocked = originalBlocked;

            return ev.Handled;
        }

        var noParryEv = new CanDoCQCEvent();
        RaiseLocalEvent(user, noParryEv);

        knowledge.Blocked = originalBlocked;

        return noParryEv.Handled;
    }

    private void AddAttackBlock(EntityUid attacker, EntityUid defender, TimeSpan duration)
    {
        var block = EnsureComp<ParryAttackBlockComponent>(attacker);
        block.BlockedTargets[defender] = _timing.CurTime + duration;
    }

    private void CleanParryAttackBlocks(EntityUid uid, ParryAttackBlockComponent component)
    {
        if (component.BlockedTargets.Count == 0)
        {
            RemCompDeferred<ParryAttackBlockComponent>(uid);
            return;
        }

        var expired = new List<EntityUid>();

        foreach (var (target, endTime) in component.BlockedTargets)
        {
            if (endTime > _timing.CurTime && !TerminatingOrDeleted(target))
                continue;

            expired.Add(target);
        }

        foreach (var target in expired)
        {
            component.BlockedTargets.Remove(target);
        }

        if (component.BlockedTargets.Count == 0)
            RemCompDeferred<ParryAttackBlockComponent>(uid);
    }

    private void UpdateCqcSuppression(Entity<ParryComponent> ent, EntityUid user)
    {
        if (!TryComp<CqcParryComponent>(ent, out var cqc) ||
            !TryComp<MartialArtsKnowledgeComponent>(user, out var knowledge) ||
            knowledge.MartialArtsForm != MartialArtsForms.CloseQuartersCombat)
        {
            return;
        }

        var active = ent.Comp.Active || ent.Comp.AlwaysActive;

        if (active)
        {
            if (!cqc.RememberedBlockedState)
            {
                cqc.HadBlockedState = knowledge.Blocked;
                cqc.RememberedBlockedState = true;
            }

            knowledge.Blocked = true;
            Dirty(user, knowledge);
            ResetComboProgress(user);
            return;
        }

        if (!cqc.RememberedBlockedState)
            return;

        knowledge.Blocked = cqc.HadBlockedState;
        cqc.RememberedBlockedState = false;
        Dirty(user, knowledge);
        ResetComboProgress(user);
    }

    private void ResetComboProgress(EntityUid uid)
    {
        if (!TryComp<CanPerformComboComponent>(uid, out var combo))
            return;

        combo.LastAttacks.Clear();
        combo.CurrentTarget = null;
        combo.BeingPerformed = default;
        combo.ConsecutiveGnashes = 0;
        combo.ResetTime = TimeSpan.Zero;
        Dirty(uid, combo);
    }

    private void RefreshAction(Entity<ParryComponent> ent)
    {
        if (ent.Comp.Action == null)
            return;

        if (HasComp<ItemComponent>(ent))
        {
            _actionContainer.EnsureAction(ent, ref ent.Comp.ActionEntity, ent.Comp.Action);
            return;
        }

        if (ent.Comp.ActionEntity == null)
            _actions.AddAction(ent, ref ent.Comp.ActionEntity, ent.Comp.Action);
    }

    private void UpdateActionToggle(Entity<ParryComponent> ent)
    {
        if (ent.Comp.ActionEntity != null)
            _actions.SetToggled(ent.Comp.ActionEntity, ent.Comp.Active || ent.Comp.AlwaysActive);
    }

    private enum ParryChannel : byte
    {
        Melee,
        Injectable,
        Thrown,
        Projectile,
    }
}
