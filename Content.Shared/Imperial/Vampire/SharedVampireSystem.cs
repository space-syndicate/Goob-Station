using Content.Shared.Actions;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.StatusEffect;
using Content.Shared.Hands.Components;
using Content.Shared.Popups;
using Content.Shared.Cuffs.Components;
using Content.Shared.Trigger;
using System.Numerics;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Collections;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Shared.Weapons.Melee;
using System.Linq;
using Content.Shared.FixedPoint;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Roles.Components;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Roles;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Robust.Shared.Physics.Events;
using Content.Shared.Directions;
using Content.Shared.Rounding;
using Content.Shared.Alert;
using Content.Shared.Damage.Systems;
using Content.Shared.Jittering;
using Content.Shared.Bed.Sleep;
using Content.Shared.Mobs.Systems;
using Content.Shared.Cuffs;
using Content.Shared.Item;
using Content.Shared.Stunnable;
using Content.Shared.Stealth.Components;
using Content.Shared.Stealth;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Store.Components;
using Content.Shared.DoAfter;
using DependencyAttribute = Robust.Shared.IoC.DependencyAttribute;
using Content.Shared.Flash;
using Content.Shared.Flash.Components;
using Content.Shared.Camera;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Content.Shared.ActionBlocker;
using System.Runtime.CompilerServices;
using Content.Shared.Mobs.Components;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Audio.Systems;
using Content.Shared.Throwing;
using Content.Shared.Standing;
using Content.Shared.Gravity;
using Content.Shared.Chemistry.ReactionEffects;

namespace Content.Shared.Imperial.Vampire;

public class SharedVampireSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionSystem = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly INetManager _net = default!;
    private HashSet<Entity<MapGridComponent>> _targetGrids = new();
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speedSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedRoleSystem _roleSystem = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;
    [Dependency] private readonly SharedJitteringSystem _jitterSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly SharedCuffableSystem _cuff = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedStealthSystem _stealth = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SleepingSystem _sleeping = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly SharedGravitySystem _gravity = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<VampireRecoveryEvent>(OnRecovery);
        SubscribeLocalEvent<VampireSwordEvent>(OnSword);
        SubscribeLocalEvent<VampireTeleportEvent>(OnTeleport);
        SubscribeLocalEvent<VampireNosferatyEvent>(OnNosferaty);
        SubscribeLocalEvent<VampireTentaclesEvent>(OnTentacles);
        SubscribeLocalEvent<VampireRushBloodEvent>(OnRushBlood);
        SubscribeLocalEvent<DamageOnContactComponent, StartCollideEvent>(OnDamageOnContactCollide);
        SubscribeLocalEvent<VampireUnCuffEvent>(OnUnCuff);
        SubscribeLocalEvent<VampireBloodTheftEvent>(OnBloodTheft);
        SubscribeLocalEvent<VampireBloodTransformEvent>(OnTransformToBlood);

        SubscribeLocalEvent<VampireBloodAnchorEvent>(OnBloodAnchorCreateStart);
        SubscribeLocalEvent<VampireComponent, VampireAnchorCreateDoAfterEvent>(OnBloodAnchorCreate);

        SubscribeLocalEvent<VampireJerkComponent, VampireJerkEvent>(OnJerk);
        SubscribeLocalEvent<VampireJerkOnContactComponent, StartCollideEvent>(OnLeaperCollide);

        SubscribeLocalEvent<VampireSwordPlusEvent>(OnSwordPlus);

        SubscribeLocalEvent<VampireComponent, VampireShadowTrapEvent>(StartOnShadowTrap);
        SubscribeLocalEvent<VampireComponent, VampireShadowTrapDoAfterEvent>(OnShadowTrap);
        SubscribeLocalEvent<VampireTrapOnTriggerComponent, StartCollideEvent>(OnVampireTrap);

        SubscribeLocalEvent<VampireSleepEvent>(OnStartSleep);
        SubscribeLocalEvent<VampireComponent, VampireSleepDoAfterEvent>(OnSleep);

        SubscribeLocalEvent<VampireReconciliationEvent>(OnStartReconciliation);
        SubscribeLocalEvent<VampireComponent, VampireReconciliationDoAfterEvent>(OnReconciliation);

        SubscribeLocalEvent<VampireInvisibleEvent>(OnInvisible);
        SubscribeLocalEvent<VampireComponent, AttemptMeleeEvent>(OnAttemptMelee);
        SubscribeLocalEvent<VampireComponent, DamageChangedEvent>(OnDamaged);

        SubscribeLocalEvent<VampireComponent, ComponentStartup>(OnVampireStartup);
        SubscribeLocalEvent<VampireComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<VampireComponent, MindRemovedMessage>(OnMindRemoved);
    }

    private void OnRecovery(VampireRecoveryEvent args)
    {
        if (!TryComp<VampireComponent>(args.Performer, out var vamp))
            return;

        if (vamp.BloodDamage + args.CostBlood >= vamp.CritThreshold)
        {
            _popup.PopupClient(Loc.GetString("vampire-popup-not-enough-blood"),
                args.Performer, args.Performer, PopupType.Medium);
            return;
        }

        if (_solutionSystem.TryGetInjectableSolution(args.Performer, out var solution, out _))
        {
            // лечим вампира, вводя ему Omnizine, TranexamicAcid
            var toInject = new Solution();
            toInject.AddReagent("Omnizine", 25f);
            toInject.AddReagent("TranexamicAcid", 5f);

            _solutionSystem.TryAddSolution(solution.Value, toInject);
        }

        if (TryComp<StaminaComponent>(args.Performer, out var stamina))
        {
            stamina.StaminaDamage = 0f;
            Dirty(args.Performer, stamina);
        }

        if (TryComp<StatusEffectsComponent>(args.Performer, out var status))
        {
            _statusEffects.TryRemoveStatusEffect(args.Performer, "Stun");
            _statusEffects.TryRemoveStatusEffect(args.Performer, "KnockedDown");
            _statusEffects.TryRemoveStatusEffect(args.Performer, "SlowedDown");
        }

        DealBloodDamage(args.Performer, args.CostBlood);
        args.Handled = true;
    }

    private void OnSword(VampireSwordEvent args)
    {
        if (!TryComp<VampireComponent>(args.Performer, out var vamp))
            return;

        if (vamp.ItemIssued)
        {
            OnIssuingSword(args.Performer);
            vamp.ClawDurationActive = TimeSpan.Zero;

            // ссылаемся на VampireSwordAction. см BaseAbilities
            _actions.SetCooldown(vamp.GrantedActions[0], vamp.CooldownSword);
        }
        else
        {
            OnIssuingSword(args.Performer);
            vamp.ClawDurationActive = _gameTiming.CurTime + vamp.ClawDuration;
        }
    }

    private void OnSwordPlus(VampireSwordPlusEvent args)
    {
        if (!TryComp<VampireComponent>(args.Performer, out var vamp))
            return;

        if (vamp.ItemIssued)
        {
            OnIssuingSword(args.Performer);
            args.Handled = true;
        }
        else
        {
            OnIssuingSword(args.Performer);
        }
    }

    /// <summary>
    /// добавляет катану в руки
    /// </summary>
    private void OnIssuingSword(EntityUid uid)
    {
        if (!TryComp<VampireComponent>(uid, out var vamp))
            return;

        if (!_net.IsServer)
            return;

        if (!vamp.ItemIssued)
        {
            _audio.PlayPvs(vamp.GetSwordSound, uid);
            var item = Spawn(vamp.SwordId, Transform(uid).Coordinates);

            // если руки не закованы в наручники, то выдаем коготь
            if (TryComp<HandsComponent>(uid, out var hands) &&
                TryComp<CuffableComponent>(uid, out var cuff))
            {
                if (!_hands.CanPickupAnyHand(uid, item, handsComp: hands))
                {
                    _hands.TryDrop(uid);
                }
                // подбираем катану
                if (_hands.TryPickup(uid, item, checkActionBlocker: false, handsComp: hands))
                {
                    vamp.ItemIssued = true;
                    Dirty(uid, vamp);
                }
                else
                {
                    // если не удалось подобрать, удаляем
                    QueueDel(item);
                    _popup.PopupClient(Loc.GetString("vampire-popup-not-hands-free"),
                        uid, uid, PopupType.Medium);
                }
            }
        }
        else
        {
            foreach (var hand in _hands.EnumerateHeld(uid))
            {
                // удаляем катану по мете
                if (MetaData(hand).EntityPrototype?.ID == vamp.SwordId)
                {
                    QueueDel(hand);
                    _audio.PlayPvs(vamp.RemoveSwordSound, uid);
                    vamp.ItemIssued = false;
                    Dirty(uid, vamp);
                    break;
                }
            }
        }
    }

    private void OnTeleport(VampireTeleportEvent args)
    {
        if (!TryComp<VampireComponent>(args.Performer, out var vamp))
            return;

        if (vamp.BloodDamage + args.CostBlood >= vamp.CritThreshold)
        {
            _popup.PopupClient(Loc.GetString("vampire-popup-not-enough-blood"),
                args.Performer, args.Performer, PopupType.Medium);
            return;
        }

        var target = vamp.TargetUser ? args.Performer : vamp.Owner;
        if (target == null || _net.IsClient || !TryComp<TransformComponent>(target, out var xform))
            return;

        var fromCoords = xform.Coordinates;
        var toCoords = VampireRandomTileInRange(xform, args.TeleportRadius);
        if (toCoords == null)
            return;

        SpawnSmokeEffect(vamp, fromCoords);

        _audio.PlayPvs(vamp.TeleportSound, args.Performer);

        _transform.SetCoordinates(target, toCoords.Value);

        DealBloodDamage(args.Performer, args.CostBlood);
        args.Handled = true;
    }

    private void SpawnSmokeEffect(VampireComponent ent, EntityCoordinates coords)
    {
        if (!coords.IsValid(EntityManager))
            return;

        var smoke = Spawn(ent.SmokePrototype, coords);

        if (TryComp<SmokeComponent>(smoke, out var smokeComp))
        {
            smokeComp.SpreadAmount = ent.SmokeRadius;
        }

        var triggerEvent = new TriggerEvent();
        RaiseLocalEvent(smoke, ref triggerEvent);
    }

    /// <summary>
    /// сжатый метод SelectRandomTileInRange
    /// </summary>
    private EntityCoordinates? VampireRandomTileInRange(TransformComponent userXform, float radius)
    {
        var userCoords = _transform.ToMapCoordinates(userXform.Coordinates);

        _targetGrids.Clear();
        _lookup.GetEntitiesInRange(userCoords, radius, _targetGrids);
        if (_targetGrids.Count == 0)
            return null;

        var targetGrid = _random.Pick(_targetGrids);

        var range = (float)Math.Sqrt(radius);
        var box = Box2.CenteredAround(userCoords.Position, new Vector2(range, range));
        var tilesInRange = _map.GetTilesEnumerator(targetGrid.Owner, targetGrid.Comp, box, false);
        var tileList = new ValueList<Vector2i>();
        while (tilesInRange.MoveNext(out var tile))
            tileList.Add(tile.GridIndices);

        if (tileList.Count == 0)
            return null;

        var tilePos = tileList[_random.Next(tileList.Count)];
        return new EntityCoordinates(targetGrid.Owner, _map.TileCenterToVector(targetGrid, tilePos));
    }

    private void OnNosferaty(VampireNosferatyEvent args)
    {
        if (!TryComp<VampireComponent>(args.Performer, out var comp))
            return;

        if (comp.BloodDamage + args.CostBlood >= comp.CritThreshold)
        {
            _popup.PopupClient(Loc.GetString("vampire-popup-not-enough-blood"),
                args.Performer, args.Performer, PopupType.Medium);
            return;
        }

        if (!TryComp<MeleeWeaponComponent>(args.Performer, out var melee)
        || !TryComp<MovementSpeedModifierComponent>(args.Performer, out var speed))
            return;

        if (comp.BuffBlocked)
        {
            _popup.PopupClient(Loc.GetString("vampire-popup-warning-ability-buff"),
            args.Performer, args.Performer, PopupType.Medium);

            return;
        }

        // сохраняем оригинальные значения скорости, урона и тд
        if (comp.OriginalDamageModifier == null)
        {
            var currentDamage = melee.Damage.DamageDict.Values.FirstOrDefault().Float();
            comp.OriginalDamageModifier = currentDamage > 0 ? currentDamage : 10f;
        }

        if (comp.OriginalAttackRate == null)
            comp.OriginalAttackRate = melee.AttackRate;

        if (comp.OriginalWalkSpeed == null)
        {
            comp.OriginalWalkSpeed = speed.BaseWalkSpeed;
            comp.OriginalSprintSpeed = speed.BaseSprintSpeed;
        }

        // выдаем бафы
        var boostedDamage = comp.OriginalDamageModifier.Value * args.DamageBoost;
        melee.Damage = new DamageSpecifier
        {
            DamageDict = new()
            {
                { "Blunt", FixedPoint2.New(boostedDamage) },
                { "Slash", FixedPoint2.New(boostedDamage) },
            }
        };

        melee.AttackRate = comp.OriginalAttackRate.Value * args.AttackRateBoost;
        Dirty(args.Performer, melee);

        _speedSystem.ChangeBaseSpeed(
            args.Performer,
            (comp.OriginalWalkSpeed ?? speed.BaseWalkSpeed) * args.BoostSpeed,
            (comp.OriginalSprintSpeed ?? speed.BaseSprintSpeed) * args.BoostSpeed,
            speed.BaseAcceleration,
            speed);

        comp.BuffBlocked = true;
        DealBloodDamage(args.Performer, args.CostBlood);
        comp.BuffBlockedUntil = _gameTiming.CurTime + args.NosferatyTime;

        if (_net.IsServer)
            _jitterSystem.DoJitter(args.Performer, args.NosferatyTime, refresh: false, amplitude: 2, frequency: 2);

        Dirty(args.Performer, comp);
        args.Handled = true;
    }

    // модифицированный OnSummonAction
    private void OnTentacles(VampireTentaclesEvent args)
    {
        if (!TryComp<VampireComponent>(args.Performer, out var vamp))
            return;

        if (vamp.BloodDamage + args.CostBlood >= vamp.CritThreshold)
        {
            _popup.PopupClient(Loc.GetString("vampire-popup-not-enough-blood"),
                args.Performer, args.Performer, PopupType.Medium);
            return;
        }

        if (args.Handled)
            return;

        var coords = args.Target;
        List<EntityCoordinates> spawnPos = new();
        spawnPos.Add(coords);

        var dirs = new List<Direction>();
        dirs.AddRange(args.OffsetDirections);

        for (var i = 0; i < 3; i++)
        {
            var dir = _random.PickAndTake(dirs);
            spawnPos.Add(coords.Offset(dir));
        }

        if (_transform.GetGrid(coords) is not { } grid || !TryComp<MapGridComponent>(grid, out var gridComp))
            return;

        foreach (var pos in spawnPos)
        {
            if (!_map.TryGetTileRef(grid, gridComp, pos, out var tileRef) ||
                _turf.IsSpace(tileRef) ||
                _turf.IsTileBlocked(tileRef, CollisionGroup.Impassable))
            {
                continue;
            }

            if (_net.IsServer)
                Spawn(args.EntityId, pos);
        }

        // ссылаемся на VampireTentaclesAction. см BaseAbilities, VampireAbilityLists.Hemomancer
        _actions.SetCooldown(vamp.GrantedActions[4], args.CooldownTentacles);
        DealBloodDamage(args.Performer, args.CostBlood);
        Dirty(args.Performer, vamp);
    }

    private void OnDamageOnContactCollide(Entity<DamageOnContactComponent> ent, ref StartCollideEvent args)
    {
        if (args.OurFixtureId != ent.Comp.FixtureId)
            return;

        var dmg = new DamageSpecifier
        {
            DamageDict = { ["Slash"] = ent.Comp.Damage }
        };

        _damage.TryChangeDamage(args.OtherEntity, dmg);
    }

    private void OnStartSleep(VampireSleepEvent args)
    {
        if (!TryComp<VampireComponent>(args.Performer, out var vamp))
            return;

        if (vamp.BloodDamage + args.CostBlood >= vamp.CritThreshold)
        {
            _popup.PopupClient(Loc.GetString("vampire-popup-not-enough-blood"),
                args.Performer, args.Performer, PopupType.Medium);
            return;
        }

        // получаем все сущности перед игроком
        var transform = Transform(vamp.Owner);
        var direction = transform.LocalRotation.GetCardinalDir();
        var frontPos = transform.Coordinates.Offset(direction.ToVec());
        var entities = _lookup.GetEntitiesInRange(frontPos, 0.5f);

        EntityUid? target = null;
        foreach (var entity in entities)
        {
            if (entity == vamp.Owner)
                continue;

            if (!HasComp<MobStateComponent>(entity))
                continue;

            target = entity;
            break;
        }

        if (target == null || !_mobStateSystem.IsAlive(target.Value))
        {
            _popup.PopupClient(Loc.GetString("vampire-popup-no-one-ahead"),
                vamp.Owner, vamp.Owner, PopupType.Medium);
            return;
        }

        // вампир не может усыпить людей с маской/солнцезащитными очками
        if (TryComp<EyeProtectionComponent>(target, out var flash))
        {
            _popup.PopupClient(Loc.GetString("vampire-popup-has-eye-protection"),
                vamp.Owner, vamp.Owner, PopupType.Medium);
            return;
        }

        // станим цель на время doAfterArgs
        _stun.TryAddStunDuration(target.Value, args.DoAfterBeforeEuthanasia);
        vamp.SleepUid = target.Value;

        var doAfterArgs = new DoAfterArgs(EntityManager, args.Performer, args.DoAfterBeforeEuthanasia,
            new VampireSleepDoAfterEvent(), args.Performer)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = false,
            BlockDuplicate = true
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
        DealBloodDamage(args.Performer, args.CostBlood);
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

        if (TryComp<SleepingComponent>(vamp.Comp.SleepUid, out var sleep))
        {
            _popup.PopupClient(Loc.GetString("vampire-popup-asleep"),
                vamp.Owner, vamp.Owner, PopupType.Medium);
            vamp.Comp.SleepUid = EntityUid.Invalid;
            return;
        }

        // ноктюрин действует не сразу, так что сначала усыпляем, а уже затем вводим
        _sleeping.TrySleeping(vamp.Comp.SleepUid);
        if (_solutionSystem.TryGetInjectableSolution(vamp.Comp.SleepUid, out var solution, out _))
        {
            var toInject = new Solution();
            toInject.AddReagent("Nocturine", 25f);

            _solutionSystem.TryAddSolution(solution.Value, toInject);
        }

        Dirty(vamp, vamp.Comp);
        args.Handled = true;
    }

    private void OnUnCuff(VampireUnCuffEvent args)
    {
        if (!TryComp<MovementSpeedModifierComponent>(args.Performer, out var speed))
            return;

        if (!TryComp<VampireComponent>(args.Performer, out var comp))
            return;

        if (comp.BloodDamage + args.CostBlood >= comp.CritThreshold)
        {
            _popup.PopupClient(Loc.GetString("vampire-popup-not-enough-blood"),
                args.Performer, args.Performer, PopupType.Medium);
            return;
        }

        if (!TryComp<CuffableComponent>(args.Performer, out var cuffComp) || cuffComp.CuffedHandCount <= 0)
        {
            _popup.PopupClient(Loc.GetString("vampire-popup-no-handcuffs"),
                args.Performer, args.Performer, PopupType.Medium);
            return;
        }

        _cuff.Uncuff(args.Performer, args.Performer, cuffComp.Container.ContainedEntities.FirstOrDefault());

        if (comp.BuffBlocked)
        {
            _popup.PopupClient(Loc.GetString("vampire-popup-warning-ability-buff"),
            args.Performer, args.Performer, PopupType.LargeCaution);

            return;
        }

        if (comp.OriginalWalkSpeed == null)
        {
            comp.OriginalWalkSpeed = speed.BaseWalkSpeed;
            comp.OriginalSprintSpeed = speed.BaseSprintSpeed;
        }

        _speedSystem.ChangeBaseSpeed(
            args.Performer,
            (comp.OriginalWalkSpeed ?? speed.BaseWalkSpeed) * args.UnCufBoostSpeed,
            (comp.OriginalSprintSpeed ?? speed.BaseSprintSpeed) * args.UnCufBoostSpeed,
            speed.BaseAcceleration,
            speed);

        comp.BuffBlocked = true;
        comp.BuffBlockedUntil = _gameTiming.CurTime + args.UnCuffBuffTime;

        if (_net.IsServer)
            _jitterSystem.DoJitter(args.Performer, args.UnCuffBuffTime, refresh: false, amplitude: 2, frequency: 2);

        DealBloodDamage(args.Performer, args.CostBlood);
        Dirty(args.Performer, comp);
        args.Handled = true;
    }

    private void OnStartReconciliation(VampireReconciliationEvent args)
    {
        if (!TryComp<VampireComponent>(args.Performer, out var vamp))
            return;

        if (vamp.BloodDamage + args.CostBlood >= vamp.CritThreshold)
        {
            _popup.PopupClient(Loc.GetString("vampire-popup-not-enough-blood"),
                args.Performer, args.Performer, PopupType.Medium);
            return;
        }

        var doAfterArgs = new DoAfterArgs(EntityManager, args.Performer, args.DoAfterBeforeReconciliation,
            new VampireReconciliationDoAfterEvent { StaminaDamage = args.ReconciliationStaminaDamage,
            DamageItem = args.ReconciliationDamageItem , KnockdownTime = args.ReconciliationKnockdownHuman}, args.Performer)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = false,
            BlockDuplicate = true
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
        DealBloodDamage(args.Performer, args.CostBlood);
        args.Handled = true;
    }

    private void OnReconciliation(Entity<VampireComponent> vamp, ref VampireReconciliationDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        // получаем все сущности перед игроком
        var transform = Transform(vamp.Owner);
        var direction = transform.LocalRotation.GetCardinalDir();
        var frontPos = transform.Coordinates.Offset(direction.ToVec());
        var entities = _lookup.GetEntitiesInRange(frontPos, 2);

        if (!entities.Any(x => x != vamp.Owner))
        {
            _popup.PopupClient(Loc.GetString("vampire-popup-no-one-around"),
                vamp.Owner, vamp.Owner, PopupType.Medium);

            return;
        }

        foreach (var entity in entities)
        {
            if (entity == vamp.Owner)
                continue;

            // если это предмет, то наносим ему ReconciliationDamageItem урона
            var IsObject = EntityManager.HasComponent<ItemComponent>(entity);
            if (IsObject)
            {
                var dmg = new DamageSpecifier();
                dmg.DamageDict["Blunt"] = FixedPoint2.New(args.DamageItem);

                _damage.TryChangeDamage(entity, dmg);
            }

            if (!_mobStateSystem.IsAlive(entity))
                continue;

            if (TryComp<StaminaComponent>(entity, out var stamina))
            {
                _stun.TryKnockdown(entity, args.KnockdownTime, force: true);
                stamina.StaminaDamage = args.StaminaDamage;
                Dirty(entity, stamina);
            }
        }

        args.Handled = true;
    }

    private void OnBloodTheft(VampireBloodTheftEvent args)
    {
        if (!TryComp<VampireComponent>(args.Performer, out var vamp))
            return;

        if (vamp.Ghouls.Count == 0)
        {
            _popup.PopupClient(Loc.GetString("vampire-popup-no-ghouls"), args.Performer, args.Performer, PopupType.Medium);
            return;
        }

        var recover = 0;

        foreach (var ghoulUid in vamp.Ghouls)
        {
            if (!TryComp<GhoulComponent>(ghoulUid, out var ghoul))
                continue;

            if (ghoul.CritThreshold - ghoul.BloodDamage < args.DamageGhoul)
                continue;

            DealGhoulBloodDamage(ghoulUid, args.DamageGhoul, ghoul);

            // восстанавливаем кровь вампиру (по 2 за каждого упыря)
            recover += 2;
        }

        if (recover <= 0)
            _popup.PopupClient(Loc.GetString("vampire-popup-ghouls-no-have-blood"), args.Performer, args.Performer, PopupType.Medium);

        if (recover > 0)
        {
            vamp.BloodDamage = Math.Max(vamp.BloodDamage - recover, 0f);
            SetBloodAlert(args.Performer, vamp);
            Dirty(args.Performer, vamp);

            _popup.PopupClient(Loc.GetString("vampire-popup-stole-blood", ("recover", recover)), args.Performer,
            args.Performer, PopupType.Medium);
        }

        args.Handled = true;
    }

    private void StartOnShadowTrap(Entity<VampireComponent> ent, ref VampireShadowTrapEvent args)
    {
        if (args.Handled)
            return;

        var (uid, vamp) = ent;
        var user = args.Performer;

        if (vamp.BloodDamage + args.CostBlood >= vamp.CritThreshold)
        {
            _popup.PopupClient(Loc.GetString("vampire-popup-not-enough-blood"),
                user, user, PopupType.Medium);
            return;
        }

        // получаем координаты места по которому кликнул игрок
        var userPos = Transform(user).Coordinates;
        var targetPos = args.Target;

        if (!userPos.TryDistance(EntityManager, targetPos, out var distance) || distance > args.Radius)
        {
            _popup.PopupClient(Loc.GetString("vampire-popup-far-away"),
                user, user, PopupType.Medium);
            return;
        }

        var doAfterArgs = new DoAfterArgs(EntityManager, user, args.DoAfterBeforeShadowTrap,
            new VampireShadowTrapDoAfterEvent { TargetCoords = GetNetCoordinates(targetPos) },
            user)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = false,
            BlockDuplicate = true
        };

        _doAfter.TryStartDoAfter(doAfterArgs);

        DealBloodDamage(args.Performer, args.CostBlood);
    }

    private void OnShadowTrap(Entity<VampireComponent> ent, ref VampireShadowTrapDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || !_net.IsServer)
            return;

        var targetPos = GetCoordinates(args.TargetCoords);
        ent.Comp.VampireUid = ent.Owner;

        Spawn("VampireTrap", targetPos);

        args.Handled = true;
    }

    private void OnVampireTrap(Entity<VampireTrapOnTriggerComponent> ent, ref StartCollideEvent args)
    {
        if (TryComp<VampireComponent>(args.OtherEntity, out var vamp))
            return;

        if (args.OurFixtureId != ent.Comp.FixtureId)
            return;

        // ослепляем жертву
        _statusEffects.TryAddStatusEffect(args.OtherEntity, TemporaryBlindnessSystem.BlindingStatusEffect,
        ent.Comp.BlindingTime, false, TemporaryBlindnessSystem.BlindingStatusEffect);

        var dmg = new DamageSpecifier
        {
            DamageDict = { ["Slash"] = ent.Comp.Damage }
        };
        _damage.TryChangeDamage(args.OtherEntity, dmg);

        _entityManager.DeleteEntity(ent.Owner);
    }

    private void OnTransformToBlood(VampireBloodTransformEvent args)
    {
        if (!TryComp<VampireComponent>(args.Performer, out var vamp))
            return;

        if (vamp.BloodDamage + args.CostBlood >= vamp.CritThreshold)
        {
            _popup.PopupClient(Loc.GetString("vampire-popup-not-enough-blood"),
                args.Performer, args.Performer, PopupType.Medium);
            return;
        }

        if (vamp.BuffBlocked)
        {
            _popup.PopupClient(Loc.GetString("vampire-popup-warning-turning-blood"),
            args.Performer, args.Performer, PopupType.Medium);

            return;
        }

        // устанавливаем коллизию такой, чтобы можно было проходить сквозь обьекты
        if (TryComp<FixturesComponent>(args.Performer, out var fixtures))
        {
            foreach (var (id, fixture) in fixtures.Fixtures)
            {
                _physics.SetHard(args.Performer, fixture, false, fixtures);
            }
        }

        VampireInvisible(args.Performer);
        vamp.BuffBlockedUntil = _gameTiming.CurTime + args.BloodTime;

        vamp.VampireIsBlood = true;

        DealBloodDamage(args.Performer, args.CostBlood);
        Dirty(args.Performer, vamp);
        args.Handled = true;
    }

    private void OnJerk(Entity<VampireJerkComponent> ent, ref VampireJerkEvent args)
    {
        if (!TryComp<VampireComponent>(args.Performer, out var vamp))
            return;

        if (vamp.BloodDamage + args.CostBlood >= vamp.CritThreshold)
        {
            _popup.PopupClient(Loc.GetString("vampire-popup-not-enough-blood"),
                args.Performer, args.Performer, PopupType.Medium);
            return;
        }

        if (_gravity.IsWeightless(args.Performer) || _standing.IsDown(args.Performer))
        {
            if (ent.Comp.JumpFailedPopup != null)
                _popup.PopupClient(Loc.GetString(ent.Comp.JumpFailedPopup.Value), args.Performer, args.Performer);
            return;
        }

        var jerkOnContact = EnsureComp<VampireJerkOnContactComponent>(ent.Owner);
        jerkOnContact.Damage = args.DamageItemOnContact;
        jerkOnContact.Knockdown = args.KnockdownDuration;

        var xform = Transform(args.Performer);
        var throwing = xform.LocalRotation.ToWorldVec() * ent.Comp.JumpDistance;
        var direction = xform.Coordinates.Offset(throwing);

        _throwing.TryThrow(args.Performer, direction, ent.Comp.JumpThrowSpeed);
        _audio.PlayPredicted(ent.Comp.JumpSound, args.Performer, args.Performer);

        DealBloodDamage(args.Performer, args.CostBlood);
        args.Handled = true;
    }

    private void OnLeaperCollide(Entity<VampireJerkOnContactComponent> ent, ref StartCollideEvent args)
    {
        if (!HasComp<MindContainerComponent>(args.OtherEntity))
        {
            var dmg = new DamageSpecifier
            {
                DamageDict = { ["Blunt"] = ent.Comp.Damage }
            };

            _damage.TryChangeDamage(args.OtherEntity, dmg);
        }
        else
        {
            _stun.TryKnockdown(args.OtherEntity, ent.Comp.Knockdown, force: true);
        }

        // задержка перед удалением компонента, чтобы обработать все столкновения
        Timer.Spawn(TimeSpan.FromSeconds(0.2), () =>
        {
            if (Exists(ent.Owner))
                RemComp<VampireJerkOnContactComponent>(ent.Owner);
        });
    }

    private void OnBloodAnchorCreateStart(VampireBloodAnchorEvent args)
    {
        if (!TryComp<VampireComponent>(args.Performer, out var vamp))
            return;

        if (!vamp.AnchorCreate)
        {
            if (vamp.BloodDamage + args.CostBlood >= vamp.CritThreshold)
            {
                _popup.PopupClient(Loc.GetString("vampire-popup-not-enough-blood"),
                    args.Performer, args.Performer, PopupType.Medium);
                return;
            }

            vamp.SpawnLocation = Transform(args.Performer).Coordinates;

            var doAfterArgs = new DoAfterArgs(EntityManager, args.Performer, args.AnchorCreateTime,
                new VampireAnchorCreateDoAfterEvent { Duration = args.DurationExistenceAnchor },
                args.Performer)
            {
                BreakOnMove = false,
                BreakOnDamage = true,
                NeedHand = true,
                BlockDuplicate = true
            };

            _doAfter.TryStartDoAfter(doAfterArgs);
            DealBloodDamage(args.Performer, args.CostBlood);
        }
        else
        {
            // проверка существования якоря
            if (vamp.VampireAnchorUid == EntityUid.Invalid || !Exists(vamp.VampireAnchorUid))
            {
                _popup.PopupClient(Loc.GetString("vampire-popup-anchor-destroyed"),
                    args.Performer, args.Performer, PopupType.LargeCaution);
                vamp.AnchorCreate = false;
            }
            else
            {
                if (_net.IsServer)
                {
                    _transform.SetCoordinates(args.Performer, Transform(vamp.VampireAnchorUid).Coordinates);
                    _audio.PlayPvs(vamp.TeleportSound, args.Performer);

                    QueueDel(vamp.VampireAnchorUid);
                }

                vamp.VampireAnchorUid = EntityUid.Invalid;
                vamp.AnchorCreate = false;
            }

            Dirty(args.Performer, vamp);
            args.Handled = true;
        }
    }

    private void OnBloodAnchorCreate(Entity<VampireComponent> ent, ref VampireAnchorCreateDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || !_net.IsServer)
            return;

        ent.Comp.VampireAnchorUid = Spawn(args.AnchorId, ent.Comp.SpawnLocation);
        ent.Comp.AnchorCreate = true;
        ent.Comp.AnchorDurationActive = _gameTiming.CurTime + args.Duration;

        Dirty(ent.Owner, ent.Comp);
        args.Handled = true;
    }

    private void OnInvisible(VampireInvisibleEvent args)
    {
        if (!TryComp<VampireComponent>(args.Performer, out var vamp))
            return;

        if (vamp.BloodDamage + args.CostBlood >= vamp.CritThreshold)
        {
            _popup.PopupClient(Loc.GetString("vampire-popup-not-enough-blood"),
                args.Performer, args.Performer, PopupType.Medium);
            return;
        }

        if (vamp.InvisibilityAbilityActive)
            vamp.InvisibilityAbilityActive = false;
        else
            vamp.InvisibilityAbilityActive = true;

        vamp.BloodLossDisguiseIsActive = args.CostBlood;
        VampireInvisible(args.Performer);

        args.Handled = true;
    }

    /// <summary>
    /// при попытке атаковать в инвизе - инвиз слетает
    /// </summary>
    private void OnAttemptMelee(EntityUid uid, VampireComponent comp, ref AttemptMeleeEvent args)
    {
        if (!TryComp<StealthComponent>(uid, out var stealth))
            return;

        if (comp.InvisibleIsActive)
        {
            VampireInvisible(uid);
        }
    }

    /// <summary>
    /// при получения урона в инвизе - инвиз слетает
    /// </summary>
    private void OnDamaged(EntityUid uid, VampireComponent comp, DamageChangedEvent args)
    {
        if (!TryComp<StealthComponent>(uid, out var stealth))
            return;

        if (comp.InvisibleIsActive && args.DamageDelta != null)
        {
            VampireInvisible(uid);
        }
    }

    /// <summary>
    /// выдает вампиру невидимость
    /// </summary>
    public void VampireInvisible(EntityUid uid)
    {
        if (!TryComp<VampireComponent>(uid, out var vamp))
            return;

        if (!vamp.InvisibleIsActive)
        {
            if (vamp.DisguiseIsActive)
            {
                _popup.PopupClient(Loc.GetString("vampire-popup-disguise-on"),
                uid, uid, PopupType.Medium);
                return;
            }

            var stealth = EnsureComp<StealthComponent>(uid);
            _stealth.SetVisibility(uid, -2f, stealth);
            _stealth.SetEnabled(uid, true, stealth);

            vamp.DisguiseIsActive = true;
            vamp.InvisibleIsActive = true;

            Dirty(uid, vamp);
        }
        else
        {
            var stealth = EnsureComp<StealthComponent>(uid);
            _stealth.SetVisibility(uid, 1f, stealth);
            _stealth.SetEnabled(uid, false, stealth);

            vamp.InvisibleIsActive = false;
            vamp.DisguiseIsActive = false;
            Dirty(uid, vamp);
        }
    }

    /// <summary>
    /// спавн лужи крови
    /// </summary>
    private void SpawnBloodPuddle(EntityUid uid, VampireComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        var coords = Transform(uid).Coordinates;

        if (_net.IsServer)
        {
            var puddle = Spawn("Puddle", coords);

            if (_solutionSystem.TryGetSolution(puddle, "puddle", out var solution))
            {
                var bloodSolution = new Solution();
                bloodSolution.AddReagent("Blood", 10f);

                _solutionSystem.TryAddSolution(solution.Value, bloodSolution);
            }
        }
    }

    private void OnRushBlood(VampireRushBloodEvent args)
    {
        if (!TryComp<MovementSpeedModifierComponent>(args.Performer, out var speed))
            return;

        if (!TryComp<VampireComponent>(args.Performer, out var comp))
            return;

        if (comp.BloodDamage + args.CostBlood >= comp.CritThreshold)
        {
            _popup.PopupClient(Loc.GetString("vampire-popup-not-enough-blood"),
                args.Performer, args.Performer, PopupType.Medium);
            return;
        }

        if (comp.BuffBlocked)
        {
            _popup.PopupClient(Loc.GetString("vampire-popup-warning-ability-buff"),
            args.Performer, args.Performer, PopupType.Medium);

            return;
        }

        if (comp.OriginalWalkSpeed == null)
        {
            comp.OriginalWalkSpeed = speed.BaseWalkSpeed;
            comp.OriginalSprintSpeed = speed.BaseSprintSpeed;
        }

        _speedSystem.ChangeBaseSpeed(
            args.Performer,
            (comp.OriginalWalkSpeed ?? speed.BaseWalkSpeed) * args.BoostSpeed,
            (comp.OriginalSprintSpeed ?? speed.BaseSprintSpeed) * args.BoostSpeed,
            speed.BaseAcceleration,
            speed);

        comp.BuffBlocked = true;
        comp.BuffBlockedUntil = _gameTiming.CurTime + args.RushBloodTime;

        if (_net.IsServer)
            _jitterSystem.DoJitter(args.Performer, args.RushBloodTime, refresh: false, amplitude: 2, frequency: 2);

        DealBloodDamage(args.Performer, args.CostBlood);
        Dirty(args.Performer, comp);
        args.Handled = true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<VampireComponent, MeleeWeaponComponent, MovementSpeedModifierComponent>();
        while (query.MoveNext(out var uid, out var comp, out var melee, out var speed))
        {
            if (comp.BuffBlocked && _gameTiming.CurTime >= comp.BuffBlockedUntil)
            {
                // т.к query распространяется и на RushBlood
                if (comp.OriginalDamageModifier != null)
                {
                    melee.Damage = new DamageSpecifier
                    {
                        DamageDict = new()
                        {
                            { "Blunt", FixedPoint2.New(comp.OriginalDamageModifier.Value) },
                            { "Slash", FixedPoint2.New(comp.OriginalDamageModifier.Value) },
                        }
                    };
                    Dirty(uid, melee);
                    comp.OriginalDamageModifier = null;

                    if (comp.OriginalAttackRate != null)
                    {
                        melee.AttackRate = comp.OriginalAttackRate.Value;
                        Dirty(uid, melee);
                        comp.OriginalAttackRate = null;
                    }
                }

                if (comp.OriginalWalkSpeed != null && comp.OriginalSprintSpeed != null)
                {
                    _speedSystem.ChangeBaseSpeed(
                        uid,
                        comp.OriginalWalkSpeed.Value,
                        comp.OriginalSprintSpeed.Value,
                        speed.BaseAcceleration,
                        speed);

                    comp.OriginalWalkSpeed = null;
                    comp.OriginalSprintSpeed = null;
                }

                comp.BuffBlocked = false;
                Dirty(uid, comp);
            }
        }

        var ghoulQuery = EntityQueryEnumerator<GhoulComponent>();
        while (ghoulQuery.MoveNext(out var uid, out var comp))
        {
            // заставляем упырей пить кровь
            if (comp.NextBloodDecay == TimeSpan.Zero)
            {
                comp.NextBloodDecay = _gameTiming.CurTime + comp.BloodDecayInterval;
                Dirty(uid, comp);
            }

            if (_gameTiming.CurTime >= comp.NextBloodDecay)
            {
                // наносим урон каждые BloodDecayInterval секунд
                DealGhoulBloodDamage(uid, comp.BloodDecayAmount, comp);
                comp.NextBloodDecay = _gameTiming.CurTime + comp.BloodDecayInterval;
                Dirty(uid, comp);

                // если урон больше количества крови, то применяем дебафы
                if (comp.BloodDamage >= comp.CritThreshold)
                {
                    if (TryComp<StaminaComponent>(uid, out var stamina))
                    {
                        var dmg = new DamageSpecifier();
                        dmg.DamageDict["Bloodloss"] = FixedPoint2.New(30);

                        _damage.TryChangeDamage(uid, dmg);
                        SpawnBloodPuddle(uid);
                        _stamina.TakeStaminaDamage(uid, 70f, stamina);

                        if (_net.IsServer)
                            _jitterSystem.DoJitter(uid, comp.ShakingTime, refresh: false, amplitude: 15f, frequency: 4f);
                    }
                }
            }
        }

        var queryInvisible = EntityQueryEnumerator<VampireComponent, StealthComponent>();
        while (queryInvisible.MoveNext(out var uid, out var vamp, out var stealth))
        {
            if (!vamp.InvisibilityAbilityActive)
                continue;

            if (vamp.BloodDamage >= vamp.CritThreshold)
            {
                VampireInvisible(uid);
                vamp.InvisibilityAbilityActive = false;
                Dirty(uid, vamp);
                continue;
            }

            if (vamp.NextBloodDecayDisguise == TimeSpan.Zero)
            {
                vamp.NextBloodDecayDisguise = _gameTiming.CurTime + vamp.BloodDecayIntervalInvisible;
                Dirty(uid, vamp);
            }

            if (_gameTiming.CurTime >= vamp.NextBloodDecayDisguise)
            {
                DealBloodDamage(uid, vamp.BloodLossDisguiseIsActive);
                vamp.NextBloodDecayDisguise = _gameTiming.CurTime + vamp.BloodDecayIntervalInvisible;
                Dirty(uid, vamp);
            }
        }

        var bloodVamp = EntityQueryEnumerator<VampireComponent, FixturesComponent>();
        while (bloodVamp.MoveNext(out var uid, out var vamp, out var fixtures))
        {
            if (!vamp.VampireIsBlood)
                continue;

            if (_gameTiming.CurTime >= vamp.NextBloodshed && vamp.VampireIsBlood)
            {
                SpawnBloodPuddle(uid);
                // оставляем кровавый след за вампиром
                vamp.NextBloodshed = _gameTiming.CurTime + TimeSpan.FromSeconds(0.1f);
                Dirty(uid, vamp);
            }

            if (_gameTiming.CurTime >= vamp.BuffBlockedUntil)
            {
                VampireInvisible(uid);
                vamp.VampireIsBlood = false;

                foreach (var (id, fixture) in fixtures.Fixtures)
                {
                    _physics.SetHard(uid, fixture, true, fixtures);
                }

                Dirty(uid, vamp);
                continue;
            }
        }

        var vampClaw = EntityQueryEnumerator<VampireComponent>();
        while (vampClaw.MoveNext(out var uid, out var vamp))
        {
            if (!_net.IsServer)
                continue;

            if (_gameTiming.CurTime >= vamp.ClawDurationActive && vamp.ItemIssued && vamp.ClawDurationActive != TimeSpan.Zero)
            {
                OnIssuingSword(uid);

                // ссылаемся на VampireSwordAction. см BaseAbilities
                _actions.SetCooldown(vamp.GrantedActions[0], vamp.CooldownSword);
                vamp.ClawDurationActive = TimeSpan.Zero;
                Dirty(uid, vamp);
            }
        }

        var vampAnchor = EntityQueryEnumerator<VampireComponent>();
        while (vampAnchor.MoveNext(out var uid, out var vamp))
        {
            if (_gameTiming.CurTime >= vamp.AnchorDurationActive && vamp.AnchorCreate)
            {
                if (_net.IsServer)
                    QueueDel(vamp.VampireAnchorUid);

                // ссылаемся на VampireJerkAction. см BaseAbilities, VampireAbilityLists.Umbrae
                _actions.SetCooldown(vamp.GrantedActions[6], vamp.CooldownBloodAnchor);
                _popup.PopupEntity(Loc.GetString("vampire-popup-anchor-destroyed"),
                uid, uid, PopupType.LargeCaution);
                vamp.AnchorCreate = false;

                Dirty(uid, vamp);
            }
        }
    }
    public void DealGhoulBloodDamage(EntityUid uid, float damage, GhoulComponent component)
    {
        component.BloodDamage = MathF.Min(component.BloodDamage + damage, component.CritThreshold);
        Dirty(uid, component);
        SetGhoulBloodAlert(uid, component);
    }

    public void SetGhoulBloodAlert(EntityUid uid, GhoulComponent component)
    {
        // вычисляем, какой должен быть спрайт в зависимости от количества крови у упыря
        var severity = ContentHelpers.RoundToLevels(
            MathF.Max(0f, component.CritThreshold - component.BloodDamage), component.CritThreshold, component.NumberBloodSections);
        _alerts.ShowAlert(uid, component.BloodAlert, (short)severity);
    }

    public void SetBloodAlert(EntityUid uid, VampireComponent? component = null)
    {
        if (!Resolve(uid, ref component, false) || component.Deleted)
            return;

        // вычисляем, какой должен быть спрайт в зависимости от количества крови у вампира
        var severity = ContentHelpers.RoundToLevels(MathF.Max(0f, component.CritThreshold - component.BloodDamage),
        component.CritThreshold, component.NumberBloodSections);
        _alerts.ShowAlert(uid, component.BloodAlert, (short)severity);
    }

    public void SetBloodCounterAlert(EntityUid uid, VampireComponent? component = null)
    {
        if (!Resolve(uid, ref component, false) || component.Deleted)
            return;

        // вычисляем, какой должен быть спрайт в зависимости от количества выпитой крови вампиром
        var severity = ContentHelpers.RoundToLevels(MathF.Max(0f, component.TotalDrunk), component.MaxDrink, component.NumberSections);
        _alerts.ShowAlert(uid, component.BloodCounterAlert, (short)severity);
    }

    public void DealBloodDamage(EntityUid uid, float damage, VampireComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        component.BloodDamage = MathF.Min(component.BloodDamage + damage, component.CritThreshold);
        Dirty(uid, component);
        SetBloodAlert(uid, component);
    }

    private void OnVampireStartup(Entity<VampireComponent> ent, ref ComponentStartup args)
    {
        if (ent.Comp.SelectingSubgroupActionEntity == null)
        {
            _actions.AddAction(ent.Owner, ref ent.Comp.SelectingSubgroupActionEntity, ent.Comp.SelectingSubgroupAction);
            Dirty(ent.Owner, ent.Comp);
        }

        // добавляем рацию
        var transmitter = EnsureComp<IntrinsicRadioTransmitterComponent>(ent.Owner);
        transmitter.Channels ??= new HashSet<ProtoId<RadioChannelPrototype>>();
        transmitter.Channels.Add(new ProtoId<RadioChannelPrototype>("VampireRadio"));

        var activeRadio = EnsureComp<ActiveRadioComponent>(ent.Owner);
        activeRadio.Channels ??= new HashSet<ProtoId<RadioChannelPrototype>>();
        activeRadio.Channels.Add(new ProtoId<RadioChannelPrototype>("VampireRadio"));

        EnsureComp<IntrinsicRadioReceiverComponent>(ent.Owner);

        // для OnJerk
        AddComp<VampireJerkComponent>(ent.Owner);

        // выдача базовых способностей
        if (ent.Comp.GrantedActions.Count == 0)
        {
            foreach (var proto in VampireAbilityLists.BaseAbilities)
            {
                EntityUid? actionEnt = null;
                _actions.AddAction(ent.Owner, ref actionEnt, proto);

                if (actionEnt != null)
                    ent.Comp.GrantedActions.Add(actionEnt.Value);
            }
        }

        SetBloodAlert(ent.Owner, ent.Comp);
        SetBloodCounterAlert(ent.Owner, ent.Comp);
    }

    private void OnMindAdded(Entity<VampireComponent> ent, ref MindAddedMessage args)
    {
        _roleSystem.MindAddRole(args.Mind, "MindRoleVampire", mind: args.Mind.Comp);
    }

    private void OnMindRemoved(Entity<VampireComponent> ent, ref MindRemovedMessage args)
    {
        _roleSystem.MindRemoveRole<VampireRoleComponent>((args.Mind.Owner, args.Mind.Comp));
    }
}
