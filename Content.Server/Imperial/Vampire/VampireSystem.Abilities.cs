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

namespace Content.Server.Imperial.Vampire;

[Virtual]
public partial class VampireSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly StunSystem _stunSystem = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;

    private void VampireAbilitiesInitialize()
    {
        SubscribeLocalEvent<VampireBatTransformEvent>(OnTransformToBat);
        SubscribeLocalEvent<VampireCloneEvent>(OnClone);
        SubscribeLocalEvent<VampireTeleportEvent>(OnTeleport);
        SubscribeLocalEvent<VampireTurnEvent>(OnTurn);
        SubscribeLocalEvent<VampireSelectingSubgroupEvent>(OnSelectingSubgroup);

        SubscribeLocalEvent<VampireSleepEvent>(OnStartSleep);
        SubscribeLocalEvent<VampireComponent, VampireSleepDoAfterEvent>(OnSleep);
    }

    private void OnTransformToBat(VampireBatTransformEvent args)
    {
        if (!TryComp<VampireComponent>(args.Performer, out var vamp))
            return;

        if (vamp.BloodDamage + args.CostBlood >= vamp.CritThreshold)
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
            EntityManager.SpawnEntity(args.BatID, playerCoords.Offset(new Vector2(i, 0)));
        }

        _polymorph.PolymorphEntity(args.Performer, args.BatConfig);

        vamp.VampireIsBat = true;
        vamp.DisguiseIsActive = true;

        _vampireSystem.DealBloodDamage(args.Performer, args.CostBlood);

        Dirty(args.Performer, vamp);
        args.Handled = true;
    }

    private void OnTeleport(VampireTeleportEvent args)
    {
        if (!TryComp<VampireComponent>(args.Performer, out var vamp))
            return;

        if (vamp.BloodDamage + args.CostBlood >= vamp.CritThreshold)
        {
            _popup.PopupEntity(Loc.GetString("vampire-popup-not-enough-blood"),
                args.Performer, args.Performer, PopupType.Medium);
            return;
        }

        var target = vamp.TargetUser ? args.Performer : args.Performer;
        if (target == null)
            return;

        var fromCoords = Transform(target).Coordinates;
        var toCoords = _vampireSystem.VampireRandomTileInRange(Transform(target), args.TeleportRadius);
        if (toCoords == null)
            return;

        _vampireSystem.SpawnSmokeEffect(vamp, fromCoords);

        _audio.PlayPvs(vamp.TeleportSound, args.Performer);

        TransformSystem.SetCoordinates(target, toCoords.Value);

        _vampireSystem.DealBloodDamage(args.Performer, args.CostBlood);
        args.Handled = true;
    }

    private void OnStartSleep(VampireSleepEvent args)
    {
        if (!TryComp<VampireComponent>(args.Performer, out var vamp))
            return;

        if (vamp.BloodDamage + args.CostBlood >= vamp.CritThreshold)
        {
            _popup.PopupEntity(Loc.GetString("vampire-popup-not-enough-blood"),
                args.Performer, args.Performer, PopupType.Medium);
            return;
        }

        // получаем все сущности перед игроком
        var transform = Transform(vamp.Owner);
        var direction = transform.LocalRotation.GetCardinalDir();
        var frontPos = transform.Coordinates.Offset(direction.ToVec());
        var target = _lookup.GetEntitiesInRange(frontPos, 0.5f, LookupFlags.Uncontained | LookupFlags.Dynamic)
        .FirstOrDefault(el => el != args.Performer && HasComp<MobStateComponent>(el));

        if (!_mobState.IsAlive(target))
        {
            _popup.PopupEntity(Loc.GetString("vampire-popup-no-one-ahead"),
                vamp.Owner, vamp.Owner, PopupType.Medium);
            return;
        }

        // вампир не может усыпить людей с маской/солнцезащитными очками
        var flashAttempt = new FlashAttemptEvent(target, args.Performer, null);
        RaiseLocalEvent(target, ref flashAttempt, true);

        if (flashAttempt.Cancelled)
        {
            _popup.PopupEntity(Loc.GetString("vampire-popup-has-eye-protection"),
                vamp.Owner, vamp.Owner, PopupType.Medium);
            return;
        }

        if (TryComp<SleepingComponent>(vamp.SleepUid, out var sleep))
        {
            _popup.PopupEntity(Loc.GetString("vampire-popup-asleep"),
                vamp.Owner, vamp.Owner, PopupType.Medium);
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
            BlockDuplicate = true
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
        _vampireSystem.DealBloodDamage(args.Performer, args.CostBlood);
    }

    private void OnSleep(Entity<VampireComponent> vamp, ref VampireSleepDoAfterEvent args)
    {
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
        if (!TryComp<VampireComponent>(args.Performer, out var vamp))
            return;

        if (!_prototypeManager.TryIndex(args.Settings, out var settings))
            return;

        if (vamp.BloodDamage + args.CostBlood >= vamp.CritThreshold)
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
        var mapCoords = TransformSystem.GetMapCoordinates(args.Performer);

        if (!_cloning.TryCloning(args.Performer, mapCoords, settings, out var cloneUid))
            return;

        // добавляем ai клону
        var htn = EnsureComp<HTNComponent>(cloneUid.Value);
        htn.RootTask = new HTNCompoundTask() { Task = "IdleCompound" };
        htn.Blackboard.SetValue(NPCBlackboard.Owner, cloneUid.Value);

        EnsureComp<ActiveNPCComponent>(cloneUid.Value);

        _vampireSystem.VampireInvisible(args.Performer);
        vamp.BuffBlockedUntil = _gameTiming.CurTime + args.InvisibilityCloneTime;
        vamp.VampireCloneIsActive = true;

        _vampireSystem.DealBloodDamage(args.Performer, args.CostBlood);

        Dirty(args.Performer, vamp);
        args.Handled = true;
    }

    private void OnTurn(VampireTurnEvent args)
    {
        if (!TryComp<VampireComponent>(args.Performer, out var vamp))
            return;

        if (vamp.VampireTurned)
        {
            _popup.PopupEntity(Loc.GetString("vampire-popup-warning-vampire-turned"),
            args.Performer, args.Performer, PopupType.Medium);

            return;
        }

        if (vamp.GhoulQuantity < args.NecessaryGhoulQuantity)
        {
            _popup.PopupEntity(Loc.GetString("vampire-popup-ghoul-quantity", ("quantity", args.NecessaryGhoulQuantity - vamp.GhoulQuantity)),
            args.Performer, args.Performer, PopupType.Medium);

            return;
        }

        switch (vamp.SelectedSubgroup)
        {
            case VampireAbilityType.Hemomancer:
                // см BaseAbilities, VampireAbilityLists.Hemomancer. Удаляется "Кровавая катана"
                _actions.RemoveAction(args.Performer, vamp.GrantedActions[0]);

                if (_prototypeManager.TryIndex<VampireAbilityListPrototype>(
                    vamp.VampireAbilitiesID[VampireAbilityType.Hemomancer], out var hemomancerAbilities))
                    _actions.AddAction(args.Performer, hemomancerAbilities.Upgrades[0]);

                break;

            case VampireAbilityType.Umbrae:
                // см BaseAbilities, VampireAbilityLists.Umbrae. Удаляется: "Переключить режим невидимости"
                _actions.RemoveAction(args.Performer, vamp.GrantedActions[5]);

                if (_prototypeManager.TryIndex<VampireAbilityListPrototype>(
                    vamp.VampireAbilitiesID[VampireAbilityType.Umbrae], out var umbraeAbilities))
                    _actions.AddAction(args.Performer, umbraeAbilities.Upgrades[0]);

                break;

            case VampireAbilityType.Gargantua:
                // см BaseAbilities, VampireAbilityLists.Gargantua. Удаляется "Гнев Носферату"
                _actions.RemoveAction(args.Performer, vamp.GrantedActions[7]);

                if (_prototypeManager.TryIndex<VampireAbilityListPrototype>(
                    vamp.VampireAbilitiesID[VampireAbilityType.Gargantua], out var gargantuaAbilities))
                    _actions.AddAction(args.Performer, gargantuaAbilities.Upgrades[0]);

                break;
        }

        vamp.VampireTurned = true;
        args.Handled = true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var queryVampBat = EntityQueryEnumerator<VampireComponent>();
        while (queryVampBat.MoveNext(out var uid, out var vamp))
        {
            if (!HasComp<PolymorphedEntityComponent>(uid) && vamp.VampireIsBat)
            {
                vamp.VampireIsBat = false;
                vamp.DisguiseIsActive = false;
                Dirty(uid, vamp);
            }
        }

        var queryClone = EntityQueryEnumerator<VampireComponent>();
        while (queryClone.MoveNext(out var uid, out var vamp))
        {
            if (_gameTiming.CurTime >= vamp.BuffBlockedUntil && vamp.VampireCloneIsActive)
            {
                _vampireSystem.VampireInvisible(uid);
                vamp.VampireCloneIsActive = false;
                Dirty(uid, vamp);
            }
        }
    }
}
