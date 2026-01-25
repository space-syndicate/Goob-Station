using Content.Shared.Chemistry.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Popups;
using System.Numerics;
using Robust.Shared.Map;
using Robust.Shared.Collections;
using Robust.Shared.Timing;
using System.Linq;
using Content.Shared.FixedPoint;
using Content.Shared.Movement.Components;
using Content.Shared.Mind.Components;
using Robust.Shared.Physics.Events;
using Content.Shared.Item;
using Content.Shared.DoAfter;
using Content.Shared.Trigger.Systems;
using Robust.Shared.Map.Components;
using Content.Shared.Physics;
using Content.Shared.Flash;

namespace Content.Shared.Imperial.Vampire;

public partial class SharedVampireSystem : EntitySystem
{
    private void GargantuaInitialize()
    {
        SubscribeLocalEvent<VampireRushBloodEvent>(OnRushBlood);

        SubscribeLocalEvent<VampireJerkComponent, VampireJerkEvent>(OnJerk);
        SubscribeLocalEvent<VampireJerkOnContactComponent, StartCollideEvent>(OnLeaperCollide);

        SubscribeLocalEvent<VampireReconciliationEvent>(OnStartReconciliation);
        SubscribeLocalEvent<VampireComponent, VampireReconciliationDoAfterEvent>(OnReconciliation);
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

        var boost = EnsureComp<VampireBuffComponent>(args.Performer);
        boost.BoostSpeed = args.BoostSpeed;
        Dirty(args.Performer, boost);

        _speedSystem.RefreshMovementSpeedModifiers(args.Performer);

        comp.BuffBlocked = true;
        comp.BuffBlockedUntil = _gameTiming.CurTime + args.RushBloodTime;

        if (_net.IsServer)
            _jitterSystem.DoJitter(args.Performer, args.RushBloodTime, refresh: false, amplitude: 2, frequency: 2);

        DealBloodDamage(args.Performer, args.CostBlood);
        Dirty(args.Performer, comp);
        args.Handled = true;
    }

    public void SpawnSmokeEffect(VampireComponent ent, EntityCoordinates coords)
    {
        if (!coords.IsValid(EntityManager))
            return;

        var smoke = Spawn(ent.SmokePrototype, coords);

        if (TryComp<SmokeComponent>(smoke, out var smokeComp))
        {
            smokeComp.SpreadAmount = ent.SmokeRadius;
        }

        RaiseLocalEvent(smoke, EntitySystem.Get<TriggerSystem>());
    }

    /// <summary>
    /// сжатый метод SelectRandomTileInRange
    /// </summary>
    public EntityCoordinates? VampireRandomTileInRange(TransformComponent userXform, float radius)
    {
        var userCoords = _transform.ToMapCoordinates(userXform.Coordinates);

        var gridUid = userXform.GridUid;
        if (gridUid == null || !TryComp<MapGridComponent>(gridUid, out var grid))
            return null;

        var box = Box2.CenteredAround(userCoords.Position, new Vector2(radius, radius));

        var gridEntity = new Entity<MapGridComponent>(gridUid.Value, grid);
        var tilesInRange = _map.GetTilesEnumerator(gridUid.Value, gridEntity, box, false);
        var tileList = new ValueList<Vector2i>();

        while (tilesInRange.MoveNext(out var tile))
        {
            // пропускаем пустые тайлы (тобеш космос)
            if (tile.Tile.IsEmpty)
                continue;

            // нет ли на тайле препятствий?
            var tilePos = _map.GridTileToLocal(gridUid.Value, gridEntity, tile.GridIndices);
            var tileMapCoords = _transform.ToMapCoordinates(tilePos);

            if (!_interaction.InRangeUnobstructed(userCoords, tileMapCoords, 0f,
                CollisionGroup.Impassable | CollisionGroup.MobMask))
                continue;

            tileList.Add(tile.GridIndices);
        }

        if (tileList.Count == 0)
            return null;

        var selectedTile = tileList[_random.Next(tileList.Count)];
        return new EntityCoordinates(gridUid.Value, _map.TileCenterToVector(gridEntity, selectedTile));
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
            DamageItem = args.ReconciliationDamageItem, KnockdownTime = args.ReconciliationKnockdownHuman,
            DamageType = args.DamageType, VampireFlashEffectID = args.VampireFlashEffectID }, args.Performer)
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

        Spawn(args.VampireFlashEffectID, Transform(vamp).Coordinates);

        foreach (var entity in entities)
        {
            if (entity == vamp.Owner)
                continue;

            if (TryComp<StaminaComponent>(entity, out var stamina))
            {
                if (!_mobStateSystem.IsAlive(entity))
                    continue;

                // вампир не может оглушить людей с маской/солнцезащитными очками
                var flashAttempt = new FlashAttemptEvent(entity, vamp.Owner, null);
                RaiseLocalEvent(entity, ref flashAttempt, true);

                if (flashAttempt.Cancelled)
                    continue;

                _stun.TryKnockdown(entity, args.KnockdownTime, force: true);
                stamina.StaminaDamage = args.StaminaDamage;
                Dirty(entity, stamina);
            }
            else
            {
                // если это предмет, то наносим ему ReconciliationDamageItem урона
                var dmg = new DamageSpecifier
                {
                    DamageDict = { [args.DamageType] = args.DamageItem }
                };

                _damage.TryChangeDamage(entity, dmg);
            }
        }

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

        var jerkOnContact = EnsureComp<VampireJerkOnContactComponent>(args.Performer);
        jerkOnContact.Damage = args.DamageItemOnContact;
        jerkOnContact.Knockdown = args.KnockdownDuration;
        jerkOnContact.DamageType = args.DamageType;
        jerkOnContact.DeletionTime = _gameTiming.CurTime + jerkOnContact.DelayDeletion;
        Dirty(args.Performer, jerkOnContact);

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
        if (args.OtherEntity == ent.Owner)
            return;

        if (!HasComp<MindContainerComponent>(args.OtherEntity) && _net.IsServer)
        {
            var dmg = new DamageSpecifier
            {
                DamageDict = { [ent.Comp.DamageType] = ent.Comp.Damage }
            };

            _damage.TryChangeDamage(args.OtherEntity, dmg);
        }
        else
        {
            _stun.TryKnockdown(args.OtherEntity, ent.Comp.Knockdown, force: true);
        }
    }

    private void GargantuaUpdate()
    {
        var query = EntityQueryEnumerator<VampireJerkOnContactComponent>();
        while (query.MoveNext(out var uid, out var jerkOnContact))
        {
            if (_gameTiming.CurTime >= jerkOnContact.DeletionTime)
                RemComp<VampireJerkOnContactComponent>(uid);
        }
    }
}
