using Content.Shared.Imperial.Vampire;
using Content.Shared.Popups;
using Content.Server.Polymorph.Components;
using System.Numerics;
using Content.Server.NPC.HTN;
using Content.Shared.NPC;
using Content.Server.NPC;
using System.Linq;
using Content.Server.Stunnable;
using DependencyAttribute = Robust.Shared.IoC.DependencyAttribute;
using Content.Shared.Bed.Sleep;
using Content.Shared.DoAfter;
using Content.Shared.StatusEffectNew;
using Content.Shared.Mobs.Components;
using Content.Shared.Flash;
using Content.Shared.Actions.Components;
using System.Runtime.CompilerServices;

namespace Content.Server.Imperial.Vampire;


public partial class VampireSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly StunSystem _stunSystem = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;

    private void VampireAbilitiesInitialize()
    {
        SubscribeLocalEvent<VampireBatTransformEvent>(OnTransformToBat);
        SubscribeLocalEvent<VampireCloneEvent>(OnClone);
        SubscribeLocalEvent<VampireTeleportEvent>(OnTeleport);
        SubscribeLocalEvent<VampireTurnEvent>(OnTurn);
        SubscribeLocalEvent<VampireSelectingSubgroupEvent>(OnSelectingSubgroup);

        SubscribeLocalEvent<VampireSleepEvent>(OnStartSleep);
        SubscribeLocalEvent<AbilityComponent, VampireSleepDoAfterEvent>(OnSleep);
    }

    private void OnTransformToBat(VampireBatTransformEvent args)
    {
        if (!TryComp<VampireComponent>(args.Performer, out var vampireComponent) && !TryComp<GhoulComponent>(args.Performer, out _))
            return;

        var vamp = EnsureComp<AbilityComponent>(args.Performer);

        if (!_vampireSystem.HasEnoughBloodShared(args.Performer, args.CostBlood))
        {
            _popup.PopupEntity(Loc.GetString("vampire-popup-not-enough-blood"),
                args.Performer, args.Performer, PopupType.Medium);
            return;
        }

        if (vamp.BuffBlocked)
        {
            _popup.PopupEntity(Loc.GetString("vampire-popup-warning-turning-bat"),
            args.Performer, args.Performer, PopupType.Medium);

            return;
        }

        if (vamp.DisguiseIsActive)
        {
            _popup.PopupEntity(Loc.GetString("vampire-popup-disguise-on"),
            args.Performer, args.Performer, PopupType.Medium);
            return;
        }

        var playerCoords = Transform(args.Performer).Coordinates;

        for (int i = 0; i < args.BatCount; i++)
        {
            var bat = Spawn(args.BatID, playerCoords.Offset(new Vector2(i, 0)));
            vamp.BatsUid.Add(bat);
        }

        _polymorph.PolymorphEntity(args.Performer, args.BatConfig);

        vamp.VampireIsBat = true;
        vamp.DisguiseIsActive = true;

        _vampireSystem.DealAbilityBloodDamageShared(args.Performer, args.CostBlood);

        Dirty(args.Performer, vamp);
        args.Handled = true;
    }

    private void OnTeleport(VampireTeleportEvent args)
    {
        if (!TryComp<VampireComponent>(args.Performer, out var vampireComponent) && !TryComp<GhoulComponent>(args.Performer, out _))
            return;

        var vamp = EnsureComp<AbilityComponent>(args.Performer);

        if (!_vampireSystem.HasEnoughBloodShared(args.Performer, args.CostBlood))
        {
            _popup.PopupEntity(Loc.GetString("vampire-popup-not-enough-blood"),
                args.Performer, args.Performer, PopupType.Medium);
            return;
        }

        var target = vamp.TargetUser ? args.Performer : args.Performer;

        var fromCoords = Transform(target).Coordinates;
        var toCoords = _vampireSystem.VampireRandomTileInRange(Transform(target), args.TeleportRadius);
        if (toCoords == null)
            return;

        _vampireSystem.SpawnSmokeEffect(vamp, fromCoords);

        _audio.PlayPvs(vamp.TeleportSound, args.Performer);

        _transformSystem.SetCoordinates(target, toCoords.Value);

        _vampireSystem.DealAbilityBloodDamageShared(args.Performer, args.CostBlood);
        args.Handled = true;
    }

    private void OnStartSleep(VampireSleepEvent args)
    {
        if (!TryComp<VampireComponent>(args.Performer, out var vampireComponent) && !TryComp<GhoulComponent>(args.Performer, out _))
            return;

        var vamp = EnsureComp<AbilityComponent>(args.Performer);

        if (!_vampireSystem.HasEnoughBloodShared(args.Performer, args.CostBlood))
        {
            _popup.PopupEntity(Loc.GetString("vampire-popup-not-enough-blood"),
                args.Performer, args.Performer, PopupType.Medium);
            return;
        }

        // получаем все сущности перед игроком
        var transform = Transform(args.Performer);
        var direction = transform.LocalRotation.GetCardinalDir();
        var frontPos = transform.Coordinates.Offset(direction.ToVec());
        var target = _lookup.GetEntitiesInRange(frontPos, 0.5f, LookupFlags.Uncontained | LookupFlags.Dynamic)
        .FirstOrDefault(el => el != args.Performer && HasComp<MobStateComponent>(el));

        if (!_mobState.IsAlive(target))
        {
            _popup.PopupEntity(Loc.GetString("vampire-popup-no-one-ahead"),
                args.Performer, args.Performer, PopupType.Medium);
            return;
        }

        // вампир не может усыпить нося маску/солнцезащитные очки
        var flashVampireAttempt = new FlashAttemptEvent(args.Performer, args.Performer, null);
        RaiseLocalEvent(args.Performer, ref flashVampireAttempt, true);

        if (flashVampireAttempt.Cancelled)
        {
            _popup.PopupEntity(Loc.GetString("vampire-popup-vampire-has-eye-protection"),
                args.Performer, args.Performer, PopupType.Medium);
            return;
        }

        // вампир не может усыпить людей с маской/солнцезащитными очками
        var flashAttempt = new FlashAttemptEvent(target, args.Performer, null);
        RaiseLocalEvent(target, ref flashAttempt, true);

        if (flashAttempt.Cancelled)
        {
            _popup.PopupEntity(Loc.GetString("vampire-popup-has-eye-protection"),
                args.Performer, args.Performer, PopupType.Medium);
            return;
        }

        if (TryComp<SleepingComponent>(vamp.SleepUid, out var sleep))
        {
            _popup.PopupEntity(Loc.GetString("vampire-popup-asleep"),
                args.Performer, args.Performer, PopupType.Medium);
            vamp.SleepUid = EntityUid.Invalid;
            return;
        }

        // станим цель на время doAfterArgs
        _stunSystem.TryAddStunDuration(target, args.DoAfterBeforeEuthanasia);
        vamp.SleepUid = target;

        var doAfterArgs = new DoAfterArgs(EntityManager, args.Performer, args.DoAfterBeforeEuthanasia,
            new VampireSleepDoAfterEvent { SleepingTime = args.SleepingTime }, args.Performer)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = false,
            BlockDuplicate = true,
            Hidden = true
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
        _vampireSystem.DealAbilityBloodDamageShared(args.Performer, args.CostBlood);
    }

    private void OnSleep(Entity<AbilityComponent> vamp, ref VampireSleepDoAfterEvent args)
    {
        if (!TryComp<VampireComponent>(vamp, out var vampireComponent) && !TryComp<GhoulComponent>(vamp, out var ghoul))
            return;

        if (args.Cancelled || args.Handled)
        {
            vamp.Comp.SleepUid = EntityUid.Invalid;
            return;
        }

        if (vamp.Comp.SleepUid == EntityUid.Invalid || vamp.Comp.SleepUid == vamp.Owner)
        {
            vamp.Comp.SleepUid = EntityUid.Invalid;
            return;
        }

        _statusEffects.TryAddStatusEffectDuration(vamp.Comp.SleepUid, SleepingSystem.StatusEffectForcedSleeping, args.SleepingTime);

        Dirty(vamp, vamp.Comp);
        args.Handled = true;
    }

    private void OnClone(VampireCloneEvent args)
    {
        if (!TryComp<VampireComponent>(args.Performer, out var vampireComponent) && !TryComp<GhoulComponent>(args.Performer, out _))
            return;

        var vamp = EnsureComp<AbilityComponent>(args.Performer);

        if (!_prototypeManager.TryIndex(args.Settings, out var settings))
            return;

        if (!_vampireSystem.HasEnoughBloodShared(args.Performer, args.CostBlood))
        {
            _popup.PopupEntity(Loc.GetString("vampire-popup-not-enough-blood"),
                args.Performer, args.Performer, PopupType.Medium);
            return;
        }

        if (vamp.BuffBlocked)
        {
            _popup.PopupEntity(Loc.GetString("vampire-popup-warning-ability-buff"),
            args.Performer, args.Performer, PopupType.Medium);

            return;
        }

        // получаем местоположение вампира, чтобы рядом с ним заспавнить клона
        var mapCoords = _transformSystem.GetMapCoordinates(args.Performer);

        if (!_cloning.TryCloning(args.Performer, mapCoords, settings, out vamp.CloneUid))
            return;

        // добавляем ai клону
        var htn = EnsureComp<HTNComponent>(vamp.CloneUid.Value);
        htn.RootTask = new HTNCompoundTask() { Task = "IdleCompound" };
        htn.Blackboard.SetValue(NPCBlackboard.Owner, vamp.CloneUid.Value);

        EnsureComp<ActiveNPCComponent>(vamp.CloneUid.Value);

        _vampireSystem.VampireInvisible(args.Performer);
        vamp.BuffBlockedUntil = _gameTiming.CurTime + args.InvisibilityCloneTime;
        vamp.VampireCloneIsActive = true;

        _vampireSystem.DealAbilityBloodDamageShared(args.Performer, args.CostBlood);

        // ссылаемся на VampireInvisibleAction. см VampireBaseAbilities, VampireUmbrae
        _actions.SetCooldown(vampireComponent!.GrantedActions[6], args.InvisibilityCloneTime * 2);

        Dirty(args.Performer, vamp);
        args.Handled = true;
    }

    private void OnTurn(VampireTurnEvent args)
    {
        if (!TryComp<VampireComponent>(args.Performer, out var vampireComponent))
            return;

        var vamp = EnsureComp<AbilityComponent>(args.Performer);

        if (vamp.VampireTurned)
        {
            _popup.PopupEntity(Loc.GetString("vampire-popup-warning-vampire-turned"),
                args.Performer, args.Performer, PopupType.Medium);

            return;
        }

        vamp.HaloUid = Spawn(vamp.HaloEffect, Transform(args.Performer).Coordinates);
        _transform.SetParent(vamp.HaloUid.Value, args.Performer);

        foreach (var ghoul in vampireComponent.Ghouls)
        {
            var ghoulAbilityComp = EnsureComp<AbilityComponent>(ghoul);
            if (!TryComp<GhoulComponent>(ghoul, out var ghoulComponent)) continue;

            EnsureComp<ActionsComponent>(ghoul);

            ghoulAbilityComp.HaloUid = Spawn(ghoulAbilityComp.HaloEffect, Transform(ghoul).Coordinates);
            _transform.SetParent(ghoulAbilityComp.HaloUid.Value, ghoul);

            var usedIndices = new HashSet<int>();
            for (int i = 0; i < vamp.GhoulBaseAbility; i++)
            {
                int abilityNumber;
                do
                {
                    abilityNumber = _random.Next(0, 3);
                } while (usedIndices.Contains(abilityNumber) || abilityNumber == 2);

                usedIndices.Add(abilityNumber);

                if (_prototypeManager.TryIndex<VampireAbilityListPrototype>(
                    vampireComponent.VampireAbilitiesID[VampireAbilityType.Base], out var baseAbilities))
                {
                    var action = _actions.AddAction(ghoul, baseAbilities.Abilities[abilityNumber]);
                    if (action != null)
                    {
                        // см BaseAbilities. Ссылаемся на "Кровавая катана"
                        if (abilityNumber == 0) ghoulComponent.GhoulVampireSwordAction = _entityManager.GetNetEntity(action);
                        ghoulComponent.GhoulGrantedActions.Add(_entityManager.GetNetEntity(action.Value));
                    }
                }
            }

            for (int i = 0; i < vamp.GhoulGroupAbility; i++)
            {
                var abilityNumber = _random.Next(0, 3);

                switch (vampireComponent.SelectedSubgroup)
                {
                    case VampireAbilityType.Hemomancer:
                        if (_prototypeManager.TryIndex<VampireAbilityListPrototype>(
                            vampireComponent.VampireAbilitiesID[VampireAbilityType.Hemomancer], out var hemomancerAbilities))
                        {
                            var action = _actions.AddAction(ghoul, hemomancerAbilities.Abilities[abilityNumber]);
                            // см BaseAbilities. Ссылаемся на "Кровавые щупальца"
                            if (abilityNumber == 5) ghoulComponent.GhoulVampireTentaclesAction = _entityManager.GetNetEntity(action);
                            if (action != null) ghoulComponent.GhoulGrantedActions.Add(_entityManager.GetNetEntity(action.Value));
                        }

                        break;

                    case VampireAbilityType.Umbrae:
                        if (_prototypeManager.TryIndex<VampireAbilityListPrototype>(
                            vampireComponent.VampireAbilitiesID[VampireAbilityType.Umbrae], out var umbraeAbilities))
                        {
                            var action = _actions.AddAction(ghoul, umbraeAbilities.Abilities[abilityNumber]);
                            // см BaseAbilities. Ссылаемся на "Кровавый якорь"
                            if (abilityNumber == 7) ghoulComponent.GhoulVampireBloodAnchorAction = _entityManager.GetNetEntity(action);
                            if (action != null) ghoulComponent.GhoulGrantedActions.Add(_entityManager.GetNetEntity(action.Value));
                        }

                        break;

                    case VampireAbilityType.Gargantua:
                        if (_prototypeManager.TryIndex<VampireAbilityListPrototype>(
                            vampireComponent.VampireAbilitiesID[VampireAbilityType.Gargantua], out var gargantuaAbilities))
                        {
                            var action = _actions.AddAction(ghoul, gargantuaAbilities.Abilities[abilityNumber]);
                            if (action != null) ghoulComponent.GhoulGrantedActions.Add(_entityManager.GetNetEntity(action.Value));
                        }

                        break;
                }
            }

            Dirty(ghoul, ghoulComponent);
        }

        switch (vampireComponent.SelectedSubgroup)
        {
            case VampireAbilityType.Hemomancer:
                // см BaseAbilities, VampireAbilityLists.Hemomancer. Удаляется "Кровавая катана"
                _actions.RemoveAction(args.Performer, vampireComponent.GrantedActions[0]);

                if (_prototypeManager.TryIndex<VampireAbilityListPrototype>(
                    vampireComponent.VampireAbilitiesID[VampireAbilityType.Hemomancer], out var hemomancerAbilities))
                    _actions.AddAction(args.Performer, hemomancerAbilities.Upgrades[0]);

                break;

            case VampireAbilityType.Umbrae:
                // см BaseAbilities, VampireAbilityLists.Umbrae. Удаляется: "Переключить режим невидимости"
                _actions.RemoveAction(args.Performer, vampireComponent.GrantedActions[6]);

                if (_prototypeManager.TryIndex<VampireAbilityListPrototype>(
                    vampireComponent.VampireAbilitiesID[VampireAbilityType.Umbrae], out var umbraeAbilities))
                    _actions.AddAction(args.Performer, umbraeAbilities.Upgrades[0]);

                break;

            case VampireAbilityType.Gargantua:
                // см BaseAbilities, VampireAbilityLists.Gargantua. Удаляется "Гнев Носферату"
                _actions.RemoveAction(args.Performer, vampireComponent.GrantedActions[8]);

                if (_prototypeManager.TryIndex<VampireAbilityListPrototype>(
                    vampireComponent.VampireAbilitiesID[VampireAbilityType.Gargantua], out var gargantuaAbilities))
                    _actions.AddAction(args.Performer, gargantuaAbilities.Upgrades[0]);

                break;
        }

        vamp.VampireTurned = true;
        Dirty(args.Performer, vamp);
        args.Handled = true;
    }

    public void AbilitiesUpdate()
    {
        var queryVampBat = EntityQueryEnumerator<AbilityComponent>();
        while (queryVampBat.MoveNext(out var uid, out var vamp))
        {
            if (!TryComp<VampireComponent>(uid, out var vampireComponent) && !TryComp<GhoulComponent>(uid, out var ghoul))
                return;

            if (!HasComp<PolymorphedEntityComponent>(uid) && vamp.VampireIsBat)
            {
                foreach (var bats in vamp.BatsUid.ToList())
                {
                    QueueDel(bats);
                    vamp.BatsUid.Remove(bats);
                }

                vamp.VampireIsBat = false;
                vamp.DisguiseIsActive = false;
                Dirty(uid, vamp);
            }
        }

        var queryClone = EntityQueryEnumerator<AbilityComponent>();
        while (queryClone.MoveNext(out var uid, out var vamp))
        {
            if (!TryComp<VampireComponent>(uid, out var vampireComponent) && !TryComp<GhoulComponent>(uid, out var ghoul))
                return;

            if (_gameTiming.CurTime >= vamp.BuffBlockedUntil && vamp.VampireCloneIsActive)
            {
                _vampireSystem.VampireInvisible(uid);
                QueueDel(vamp.CloneUid);
                vamp.VampireCloneIsActive = false;
                Dirty(uid, vamp);
            }
        }
    }
}
