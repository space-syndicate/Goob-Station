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


namespace Content.Shared.Imperial.Vampire;

public sealed class VampireSystem : EntitySystem
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
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedRoleSystem _roleSystem = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;
    [Dependency] private readonly SharedJitteringSystem _jitterSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly SharedCuffableSystem _cuff = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<VampireRecoveryEvent>(OnRecovery);
        SubscribeLocalEvent<VampireClawEvent>(OnIssuingClaw);
        SubscribeLocalEvent<VampireTeleportEvent>(OnTeleport);
        SubscribeLocalEvent<VampireNosferatyEvent>(OnNosferaty);
        SubscribeLocalEvent<VampireTentaclesEvent>(OnTentacles);
        SubscribeLocalEvent<VampireRushBloodEvent>(OnRushBlood);
        SubscribeLocalEvent<DamageOnContactComponent, StartCollideEvent>(OnDamadeOnContactCollide);
        SubscribeLocalEvent<VampireSleepEvent>(OnSleep);
        SubscribeLocalEvent<VampireUnCuffEvent>(OnUnCuff);
        SubscribeLocalEvent<VampireReconciliationEvent>(OnReconciliation);
        SubscribeLocalEvent<VampireBloodTheftEvent>(OnBloodTheft);

        SubscribeLocalEvent<VampireComponent, ComponentStartup>(OnVampireStartup);
        SubscribeLocalEvent<VampireComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<VampireComponent, MindRemovedMessage>(OnMindRemoved);
    }

    private void OnRecovery(VampireRecoveryEvent args)
    {
        var performer = args.Performer;
        TryComp<VampireComponent>(performer, out var vamp);

        if (vamp!.BloodDamage + args.CostBlood >= vamp!.CritThreshold)
        {
            _popup.PopupClient(Loc.GetString("Вам не хватает крови!"),
                args.Performer, args.Performer, PopupType.Medium);
            return;
        }

        if (_solutionSystem.TryGetInjectableSolution(performer, out var solution, out _))
        {
            // лечим вампира, вводя ему Omnizine, TranexamicAcid
            var toInject = new Solution();
            toInject.AddReagent("Omnizine", 25f);
            toInject.AddReagent("TranexamicAcid", 5f);

            _solutionSystem.TryAddSolution(solution.Value, toInject);
        }

        if (TryComp<StaminaComponent>(performer, out var stamina))
        {
            stamina.StaminaDamage = 0f;
            Dirty(performer, stamina);
        }

        if (TryComp<StatusEffectsComponent>(performer, out var status))
        {
            _statusEffects.TryRemoveStatusEffect(performer, "Stun");
            _statusEffects.TryRemoveStatusEffect(performer, "KnockedDown");
            _statusEffects.TryRemoveStatusEffect(performer, "SlowedDown");
        }

        DealBloodDamage(args.Performer, args.CostBlood);
        args.Handled = true;
    }

    /// <summary>
    /// добавляет когти в руки
    /// </summary>
    private void OnIssuingClaw(VampireClawEvent args)
    {
        var performer = args.Performer;

        if (!TryComp<VampireComponent>(performer, out var comp))
            return;

        if (!comp.ItemIssued)
        {
            var item = Spawn(comp.ClawId, Transform(performer).Coordinates);

            // если руки не закованы в наручники, то выдаем коготь
            if (TryComp<HandsComponent>(performer, out var hands) &&
                TryComp<CuffableComponent>(performer, out var cuff))
            {
                if (_hands.CanPickupAnyHand(performer, item, handsComp: hands))
                {
                    if (!_hands.CanPickupAnyHand(performer, item, handsComp: hands))
                    {
                        // выбрасываем предмет, если руки заняты
                        _hands.TryDrop(performer);
                    }

                    _hands.TryPickup(performer, item, checkActionBlocker: false, handsComp: hands);
                    comp.ItemIssued = true;
                    Dirty(performer, comp);
                }
            }
        }
        else
        {
            foreach (var hand in _hands.EnumerateHeld(performer))
            {
                // удаляем когти по мете
                if (MetaData(hand).EntityPrototype?.ID == comp.ClawId)
                {
                    QueueDel(hand);
                    comp.ItemIssued = false;
                    Dirty(performer, comp);
                    break;
                }
            }
        }

        args.Handled = true;
    }

    private void OnTeleport(VampireTeleportEvent args)
    {
        if (!TryComp<VampireComponent>(args.Performer, out var vamp))
            return;

        if (vamp.BloodDamage + args.CostBlood >= vamp.CritThreshold)
        {
            _popup.PopupClient(Loc.GetString("Вам не хватает крови!"),
                args.Performer, args.Performer, PopupType.Medium);
            return;
        }

        var target = vamp.TargetUser ? args.Performer : vamp.Owner;
        if (target == null || _net.IsClient || !TryComp<TransformComponent>(target, out var xform))
            return;

        var fromCoords = xform.Coordinates;
        var toCoords = VampireRandomTileInRange(xform, vamp.TeleportRadius);
        if (toCoords == null)
            return;

        SpawnSmokeEffect(vamp, fromCoords);
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

        var targetGrid = _random.GetRandom().Pick(_targetGrids);

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
            _popup.PopupClient(Loc.GetString("Вам не хватает крови!"),
                args.Performer, args.Performer, PopupType.Medium);
            return;
        }

        if (!TryComp<MeleeWeaponComponent>(args.Performer, out var melee))
            return;

        if (!TryComp<MovementSpeedModifierComponent>(args.Performer, out var speed))
            return;

        if (comp.BuffBlocked)
        {
            _popup.PopupClient(Loc.GetString("Вы не можете одновременно активировать несколько бафф способностей"),
            args.Performer, args.Performer, PopupType.LargeCaution);

            return;
        }

        if (_gameTiming.CurTime < comp.BuffBlockedUntil)
            return;

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
        var boostedDamage = comp.OriginalDamageModifier.Value * comp.DamageBoost;
        melee.Damage = new DamageSpecifier
        {
            DamageDict = new()
            {
                { "Blunt", FixedPoint2.New(boostedDamage) },
                { "Slash", FixedPoint2.New(boostedDamage) },
            }
        };
        Dirty(args.Performer, melee);

        melee.AttackRate = comp.OriginalAttackRate.Value * comp.AttackRateBoost;
        Dirty(args.Performer, melee);

        _speedSystem.ChangeBaseSpeed(
            args.Performer,
            (comp.OriginalWalkSpeed ?? speed.BaseWalkSpeed) * comp.BoostSpeed,
            (comp.OriginalSprintSpeed ?? speed.BaseSprintSpeed) * comp.BoostSpeed,
            speed.BaseAcceleration,
            speed);

        comp.BuffBlocked = true;
        DealBloodDamage(args.Performer, args.CostBlood);
        comp.BuffBlockedUntil = _gameTiming.CurTime + TimeSpan.FromSeconds(25f);

        Dirty(args.Performer, comp);
        args.Handled = true;
    }

    // модифицированный OnSummonAction
    private void OnTentacles(VampireTentaclesEvent args)
    {
        TryComp<VampireComponent>(args.Performer, out var vamp);

        if (vamp!.BloodDamage + args.CostBlood >= vamp!.CritThreshold)
        {
            _popup.PopupClient(Loc.GetString("Вам не хватает крови!"),
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
        DealBloodDamage(args.Performer, args.CostBlood);
        Dirty(args.Performer, vamp!);
    }

    private void OnDamadeOnContactCollide(Entity<DamageOnContactComponent> ent, ref StartCollideEvent args)
    {
        if (args.OurFixtureId != ent.Comp.FixtureId)
            return;

        var dmg = new DamageSpecifier
        {
            DamageDict = { ["Slash"] = ent.Comp.Damage }
        };

        _damage.TryChangeDamage(args.OtherEntity, dmg);
    }

    private void OnSleep(VampireSleepEvent args)
    {
        if (!TryComp<VampireComponent>(args.Performer, out var comp))
            return;

        if (comp.BloodDamage + args.CostBlood >= comp.CritThreshold)
        {
            _popup.PopupClient(Loc.GetString("Вам не хватает крови!"),
                args.Performer, args.Performer, PopupType.Medium);
            return;
        }

        // получаем все сущности перед игроком
        var transform = Transform(args.Performer);
        var direction = transform.LocalRotation.GetCardinalDir();
        var frontPos = transform.Coordinates.Offset(direction.ToVec());
        var entities = _lookup.GetEntitiesInRange(frontPos, 0.5f);

        if (!entities.Any(x => x != args.Performer))
        {
            _popup.PopupClient(Loc.GetString("Впереди никого нет!"),
                args.Performer, args.Performer, PopupType.Medium);

            return;
        }

        foreach (var entity in entities)
        {
            if (entity == args.Performer)
                continue;

            if (TryComp<SleepingComponent>(entity, out var sleep))
            {
                _popup.PopupClient(Loc.GetString("Уже спит!"),
                    args.Performer, args.Performer, PopupType.Medium);
                continue;
            }

            if (!_mobStateSystem.IsAlive(entity))
                continue;

            if (_solutionSystem.TryGetInjectableSolution(entity, out var solution, out _))
            {
                var toInject = new Solution();
                toInject.AddReagent("Nocturine", 25f);

                _solutionSystem.TryAddSolution(solution.Value, toInject);

                continue;
            }
        }

        DealBloodDamage(args.Performer, args.CostBlood);
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
            _popup.PopupClient(Loc.GetString("Вам не хватает крови!"),
                args.Performer, args.Performer, PopupType.Medium);
            return;
        }

        if (!TryComp<CuffableComponent>(args.Performer, out var cuffComp) || cuffComp.CuffedHandCount <= 0)
        {
            _popup.PopupClient(Loc.GetString("На вас не надеты наручники!"),
                args.Performer, args.Performer, PopupType.Medium);
            return;
        }

        _cuff.Uncuff(args.Performer, args.Performer, cuffComp.LastAddedCuffs);

        if (comp.BuffBlocked)
        {
            _popup.PopupClient(Loc.GetString("Вы не можете одновременно активировать несколько бафф способностей"),
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
            (comp.OriginalWalkSpeed ?? speed.BaseWalkSpeed) * args.BoostSpeed,
            (comp.OriginalSprintSpeed ?? speed.BaseSprintSpeed) * args.BoostSpeed,
            speed.BaseAcceleration,
            speed);

        comp.BuffBlocked = true;
        comp.BuffBlockedUntil = _gameTiming.CurTime + TimeSpan.FromSeconds(6f);

        DealBloodDamage(args.Performer, 90f);
        Dirty(args.Performer, comp);
        args.Handled = true;
    }

    private void OnReconciliation(VampireReconciliationEvent args)
    {
        if (!TryComp<VampireComponent>(args.Performer, out var comp))
            return;

        if (comp.BloodDamage + args.CostBlood >= comp.CritThreshold)
        {
            _popup.PopupClient(Loc.GetString("Вам не хватает крови!"),
                args.Performer, args.Performer, PopupType.Medium);
            return;
        }

        // получаем все сущности перед игроком
        var transform = Transform(args.Performer);
        var direction = transform.LocalRotation.GetCardinalDir();
        var frontPos = transform.Coordinates.Offset(direction.ToVec());
        var entities = _lookup.GetEntitiesInRange(frontPos, 1.5f);

        if (!entities.Any(x => x != args.Performer))
        {
            _popup.PopupClient(Loc.GetString("Рядом никого нет!"),
                args.Performer, args.Performer, PopupType.Medium);

            return;
        }

        foreach (var entity in entities)
        {
            if (entity == args.Performer)
                continue;

            // если это предмет, то наносим ему 20 урона
            bool IsObject = EntityManager.HasComponent<ItemComponent>(entity);
            if (IsObject)
            {
                var dmg = new DamageSpecifier();
                dmg.DamageDict["Blunt"] = FixedPoint2.New(20);

                _damage.TryChangeDamage(entity, dmg);
            }

            if (!_mobStateSystem.IsAlive(entity))
                continue;

            if (TryComp<StaminaComponent>(entity, out var stamina))
            {
                _stun.TryKnockdown(entity, TimeSpan.FromSeconds(3), force: true);
                stamina.StaminaDamage = 100f;
                Dirty(entity, stamina);
            }
        }

        DealBloodDamage(args.Performer, args.CostBlood);
        args.Handled = true;
    }

    private void OnBloodTheft(VampireBloodTheftEvent args)
    {
        if (!TryComp<VampireComponent>(args.Performer, out var vamp))
            return;

        if (vamp.Ghouls.Count == 0)
        {
            _popup.PopupClient(Loc.GetString("У вас нет упырей!"), args.Performer, args.Performer, PopupType.Medium);
            return;
        }

        var recover = 0;

        foreach (var ghoulUid in vamp.Ghouls)
        {
            if (!TryComp<GhoulComponent>(ghoulUid, out var ghoul))
                continue;

            if (ghoul.CritThreshold - ghoul.BloodDamage < args.CostBlood)
            {
                _popup.PopupClient(Loc.GetString("У ваших упырей недостаточно крови!"), args.Performer, args.Performer, PopupType.Medium);
                continue;
            }

            DealGhoulBloodDamage(ghoulUid, args.CostBlood, ghoul);

            // восстанавливаем кровь вампиру (по 2 за каждого упыря)
            recover += 2;
        }

        if (recover > 0)
        {
            vamp.BloodDamage = Math.Max(vamp.BloodDamage - recover, 0f);
            SetBloodAlert(args.Performer, vamp);
            Dirty(args.Performer, vamp);

            _popup.PopupClient(Loc.GetString($"Вы украли {recover} единиц крови у своих упырей!"), args.Performer,
            args.Performer, PopupType.Medium);
        }

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
                }

                if (comp.OriginalAttackRate != null)
                {
                    melee.AttackRate = comp.OriginalAttackRate.Value;
                    Dirty(uid, melee);
                    comp.OriginalAttackRate = null;
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
                _popup.PopupClient(Loc.GetString("STOP"), uid, uid, PopupType.LargeCaution);
                comp.BuffBlocked = false;
                Dirty(uid, comp);
            }
        }

        var bloodQuery = EntityQueryEnumerator<VampireComponent>();
        while (bloodQuery.MoveNext(out var uid2, out var comp2))
        {
            if (comp2.NextBloodDecay == TimeSpan.Zero)
            {
                comp2.NextBloodDecay = _gameTiming.CurTime + comp2.BloodDecayInterval;
                Dirty(uid2, comp2);
            }

            if (_gameTiming.CurTime >= comp2.NextBloodDecay)
            {
                DealBloodDamage(uid2, comp2.BloodDecayAmount);
                comp2.NextBloodDecay = _gameTiming.CurTime + comp2.BloodDecayInterval;
                Dirty(uid2, comp2);

                if (comp2.BloodDamage >= comp2.CritThreshold)
                {
                    if (TryComp<StaminaComponent>(uid2, out var stamina))
                    {
                        var dmg = new DamageSpecifier();
                        dmg.DamageDict["Bloodloss"] = FixedPoint2.New(15);

                        _damage.TryChangeDamage(uid2, dmg);
                        SpawnBloodPuddle(uid2);
                        _stamina.TakeStaminaDamage(uid2, 70f, stamina);
                        _jitterSystem.DoJitter(uid2, comp2.ShakingTime, refresh: true, amplitude: 40f, frequency: 10f);
                    }
                }
            }
        }

        var ghoulQuery = EntityQueryEnumerator<GhoulComponent>();
        while (ghoulQuery.MoveNext(out var ghoulUid, out var ghoulComp))
        {
            if (ghoulComp.NextBloodDecay == TimeSpan.Zero)
            {
                ghoulComp.NextBloodDecay = _gameTiming.CurTime + ghoulComp.BloodDecayInterval;
                Dirty(ghoulUid, ghoulComp);
            }

            if (_gameTiming.CurTime >= ghoulComp.NextBloodDecay)
            {
                // наносим урон каждые BloodDecayInterval секунд
                DealGhoulBloodDamage(ghoulUid, ghoulComp.BloodDecayAmount, ghoulComp);
                ghoulComp.NextBloodDecay = _gameTiming.CurTime + ghoulComp.BloodDecayInterval;
                Dirty(ghoulUid, ghoulComp);

                // если урон больше количества крови, то применяем дебафы
                if (ghoulComp.BloodDamage >= ghoulComp.CritThreshold)
                {
                    if (TryComp<StaminaComponent>(ghoulUid, out var stamina))
                    {
                        var dmg = new DamageSpecifier();
                        dmg.DamageDict["Bloodloss"] = FixedPoint2.New(30);

                        _damage.TryChangeDamage(ghoulUid, dmg);
                        SpawnBloodPuddle(ghoulUid);
                        _stamina.TakeStaminaDamage(ghoulUid, 70f, stamina);
                        _jitterSystem.DoJitter(ghoulUid, ghoulComp.ShakingTime, refresh: true, amplitude: 15f, frequency: 4f);
                    }
                }
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
            MathF.Max(0f, component.CritThreshold - component.BloodDamage),
            component.CritThreshold,
            7);
        _alerts.ShowAlert(uid, component.BloodAlert, (short)severity);
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
                bloodSolution.AddReagent("Blood", 50f);

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
            _popup.PopupClient(Loc.GetString("Вам не хватает крови!"),
                args.Performer, args.Performer, PopupType.Medium);
            return;
        }

        if (comp.BuffBlocked)
        {
            _popup.PopupClient(Loc.GetString("Вы не можете одновременно активировать несколько бафф способностей"),
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
            (comp.OriginalWalkSpeed ?? speed.BaseWalkSpeed) * args.BoostSpeed,
            (comp.OriginalSprintSpeed ?? speed.BaseSprintSpeed) * args.BoostSpeed,
            speed.BaseAcceleration,
            speed);

        comp.BuffBlocked = true;
        comp.BuffBlockedUntil = _gameTiming.CurTime + TimeSpan.FromSeconds(6f);

        DealBloodDamage(args.Performer, args.CostBlood);
        Dirty(args.Performer, comp);
        args.Handled = true;
    }

    public void SetBloodAlert(EntityUid uid, VampireComponent? component = null)
    {
        if (!Resolve(uid, ref component, false) || component.Deleted)
            return;

        // вычисляем, какой должен быть спрайт в зависимости от количества крови у вампира
        var severity = ContentHelpers.RoundToLevels(MathF.Max(0f, component.CritThreshold - component.BloodDamage), component.CritThreshold, 7);
        _alerts.ShowAlert(uid, component.BloodAlert, (short)severity);
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
        if (_mind.TryGetMind(ent.Owner, out var mindId, out var mind))
        {
            if (!_roleSystem.MindHasRole<VampireRoleComponent>(mindId))
                _roleSystem.MindAddRole(mindId, "MindRoleVampire", mind: mind);
        }

        if (ent.Comp.GrimoreActionEntity == null)
        {
            _actions.AddAction(ent.Owner, ref ent.Comp.GrimoreActionEntity, ent.Comp.GrimoreAction);
            Dirty(ent.Owner, ent.Comp);
        }
        SetBloodAlert(ent.Owner, ent.Comp);
    }

    private void OnMindAdded(Entity<VampireComponent> ent, ref MindAddedMessage args)
    {
        if (!_roleSystem.MindHasRole<VampireRoleComponent>(args.Mind))
            _roleSystem.MindAddRole(args.Mind, "MindRoleVampire", mind: args.Mind.Comp);

        if (ent.Comp.GrimoreActionEntity == null)
        {
            _actions.AddAction(ent.Owner, ref ent.Comp.GrimoreActionEntity, ent.Comp.GrimoreAction);
            Dirty(ent.Owner, ent.Comp);
        }
        SetBloodAlert(ent.Owner, ent.Comp);
    }

    private void OnMindRemoved(Entity<VampireComponent> ent, ref MindRemovedMessage args)
    {
        _roleSystem.MindRemoveRole<VampireRoleComponent>((args.Mind.Owner, args.Mind.Comp));
    }
}
