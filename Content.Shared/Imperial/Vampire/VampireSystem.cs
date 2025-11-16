using Content.Shared.Actions;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Imperial.Vampire;
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
using Content.Shared.Store;
using Content.Shared.Roles.Components;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Roles;

namespace Content.Server.Imperial.Vampire;

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

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<VampireRecoveryEvent>(OnRecovery);
        SubscribeLocalEvent<VampireClawEvent>(OnIssuingClaw);
        SubscribeLocalEvent<VampireTeleportEvent>(OnTeleport);
        SubscribeLocalEvent<VampireNosferatyEvent>(OnNosferaty);
        SubscribeLocalEvent<VampireGrimoireEvent>(OnGrimoireActivated);

        SubscribeLocalEvent<VampireComponent, ComponentStartup>(OnVampireStartup);
        SubscribeLocalEvent<VampireComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<VampireComponent, MindRemovedMessage>(OnMindRemoved);
    }

    private void OnGrimoireActivated(VampireGrimoireEvent args)
    {
        var performer = args.Performer;

        if (!TryComp<VampireComponent>(performer, out var vamp))
            return;

        if (vamp.GrimoreActionEntity == null)
            return;

        _uiSystem.TryToggleUi(vamp.GrimoreActionEntity.Value, StoreUiKey.Key, performer);

        args.Handled = true;
    }

    private void OnRecovery(VampireRecoveryEvent args)
    {
        var performer = args.Performer;

        if (_solutionSystem.TryGetInjectableSolution(performer, out var solution, out _))
        {
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

        args.Handled = true;
    }

    private void OnIssuingClaw(VampireClawEvent args)
    {
        var performer = args.Performer;

        if (!TryComp<VampireComponent>(performer, out var comp))
            return;

        if (!comp.ItemIssued)
        {
            var item = Spawn(comp.ClawId, Transform(performer).Coordinates);

            if (TryComp<HandsComponent>(performer, out var hands) &&
                TryComp<CuffableComponent>(performer, out var cuff))
            {
                if (_hands.CanPickupAnyHand(performer, item, handsComp: hands))
                {
                    if (!_hands.CanPickupAnyHand(performer, item, handsComp: hands))
                    {
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
        if (!TryComp<VampireComponent>(args.Performer, out var ent))
            return;

        var target = ent.TargetUser ? args.Performer : ent.Owner;
        if (target == null || _net.IsClient || !TryComp<TransformComponent>(target, out var xform))
            return;

        var fromCoords = xform.Coordinates;
        var toCoords = VampireRandomTileInRange(xform, ent.TeleportRadius);
        if (toCoords == null)
            return;

        SpawnSmokeEffect(ent, fromCoords);
        _transform.SetCoordinates(target, toCoords.Value);

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
    /// сжатый код визов
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

        if (!TryComp<MeleeWeaponComponent>(args.Performer, out var melee))
            return;

        if (!TryComp<MovementSpeedModifierComponent>(args.Performer, out var speed))
            return;

        if (comp.BuffBlocked && _gameTiming.CurTime < comp.BuffBlockedUntil)
            return;

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

        _popup.PopupClient(Loc.GetString("GO"), args.Performer, args.Performer, PopupType.LargeCaution);
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
        comp.BuffBlockedUntil = _gameTiming.CurTime + TimeSpan.FromSeconds(25f);

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
    }

    private void OnMindRemoved(Entity<VampireComponent> ent, ref MindRemovedMessage args)
    {
        _roleSystem.MindRemoveRole<VampireRoleComponent>((args.Mind.Owner, args.Mind.Comp));
    }
}
