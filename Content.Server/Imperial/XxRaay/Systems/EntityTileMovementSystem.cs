using System.Numerics;
using Content.Shared.CombatMode;
using Content.Shared.Doors;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Imperial.XxRaay.Components;
using Content.Shared.Imperial.XxRaay.Systems;
using Content.Shared.Maps;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Physics;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Server.Imperial.XxRaay.Systems;

public sealed class EntityTileMovementSystem : SharedEntityTileMovementSystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly SharedPhysicsSystem _physicsSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedDoorSystem _doorSystem = default!;
    [Dependency] private readonly SharedCombatModeSystem _combatModeSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        UpdatesBefore.Add(typeof(SharedMoverController));
        SubscribeLocalEvent<EntityTileMovementComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<EntityTileMovementComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<EntityTileMovementComponent, MoveInputEvent>(OnMoveInput);
    }
    
    private void OnMoveInput(Entity<EntityTileMovementComponent> entity, ref MoveInputEvent args)
    {
        entity.Comp.LastMoveButtons = args.Entity.Comp.HeldMoveButtons;
    }

    private void OnComponentInit(EntityUid uid, EntityTileMovementComponent component, ComponentInit args)
    {
        component.LastMoveTime = _gameTiming.CurTime;
    }

    private void OnComponentShutdown(EntityUid uid, EntityTileMovementComponent component, ComponentShutdown args)
    {
        if (!TryComp<InputMoverComponent>(uid, out var mover))
            return;

        mover.CanMove = true;
        Dirty(uid, mover);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<EntityTileMovementComponent, InputMoverComponent, TransformComponent>();
        var currentTime = _gameTiming.CurTime;

        while (query.MoveNext(out var uid, out var tileMovement, out var mover, out var xform))
        {
            if (!tileMovement.Enabled)
            {
                if (!mover.CanMove)
                {
                    mover.CanMove = true;
                    Dirty(uid, mover);
                }
                continue;
            }

            var moveButtons = tileMovement.LastMoveButtons != MoveButtons.None 
                ? tileMovement.LastMoveButtons 
                : mover.HeldMoveButtons;
            
            if ((moveButtons & MoveButtons.AnyDirection) == MoveButtons.None)
            {
                if (!mover.CanMove)
                {
                    mover.CanMove = true;
                    Dirty(uid, mover);
                }
                StopMovement(uid, mover);
                continue;
            }

            if (mover.CanMove)
            {
                mover.CanMove = false;
                Dirty(uid, mover);
            }

            var wishDir = GetDirectionFromButtons(moveButtons);
            if (wishDir == Vector2.Zero)
            {
                if (!mover.CanMove)
                {
                    mover.CanMove = true;
                    Dirty(uid, mover);
                }
                StopMovement(uid, mover);
                continue;
            }

            var isWalking = (moveButtons & MoveButtons.Walk) != 0;
            var moveSpeed = GetMoveSpeed(uid, isWalking);
            var moveDelay = 1.0f / moveSpeed;
            var timeSinceLastMove = currentTime - tileMovement.LastMoveTime;

            if (timeSinceLastMove.TotalSeconds < moveDelay)
                continue;

            var moveDirection = GetTileDirection(wishDir);
            if (moveDirection == Vector2i.Zero)
                continue;

            ProcessMovement(uid, tileMovement, mover, xform, moveDirection, wishDir, currentTime);
        }
    }

    private void ProcessMovement(EntityUid uid, EntityTileMovementComponent tileMovement, InputMoverComponent mover, 
        TransformComponent xform, Vector2i moveDirection, Vector2 wishDir, TimeSpan currentTime)
    {
        var moveResult = TryMoveToTile(uid, moveDirection, xform, wishDir);
        
        if (moveResult.Moved)
        {
            tileMovement.LastMoveTime = currentTime;
            tileMovement.PendingMoveDirection = null;
            StopMovement(uid, mover);
            return;
        }

        if (moveResult.DoorOpening)
        {
            tileMovement.LastMoveTime = currentTime;
            StopMovement(uid, mover);
            return;
        }

        if (moveResult.BlockedByWall)
        {
            HandleWallBlock(uid, tileMovement, mover, xform, wishDir, currentTime);
            return;
        }

        tileMovement.PendingMoveDirection = moveDirection;
    }

    private void HandleWallBlock(EntityUid uid, EntityTileMovementComponent tileMovement, InputMoverComponent mover,
        TransformComponent xform, Vector2 wishDir, TimeSpan currentTime)
    {
        if (!TryComp<MapGridComponent>(xform.GridUid, out var grid))
            return;

        var currentTile = _mapSystem.LocalToTile(xform.GridUid.Value, grid, xform.Coordinates);
        var currentTileCheck = CanMoveToTile(uid, xform.GridUid.Value, grid, currentTile);
        
        if (!currentTileCheck.CanMove)
            return;

        var currentTileCoords = _mapSystem.ToCenterCoordinates(xform.GridUid.Value, currentTile, grid);
        var distance = (xform.Coordinates.Position - currentTileCoords.Position).Length();
        var moveAngle = wishDir.ToWorldAngle();
        
        _transformSystem.SetLocalRotation(uid, moveAngle, xform);
        
        if (distance > 0.05f)
            _transformSystem.SetCoordinates(uid, xform, currentTileCoords);
        
        tileMovement.LastMoveTime = currentTime;
        StopMovement(uid, mover);
    }


    private MoveResult TryMoveToTile(EntityUid uid, Vector2i direction, TransformComponent xform, Vector2 wishDir)
    {
        if (xform.GridUid == null || !TryComp<MapGridComponent>(xform.GridUid, out var grid))
            return new MoveResult(false, false);

        var currentTile = _mapSystem.LocalToTile(xform.GridUid.Value, grid, xform.Coordinates);
        var targetTile = currentTile + direction;

        if (!_mapSystem.TryGetTileRef(xform.GridUid.Value, grid, targetTile, out _))
            return new MoveResult(false, false);

        var currentTileDoorResult = CheckDoorsOnTile(uid, xform.GridUid.Value, grid, currentTile);
        if (currentTileDoorResult.HasValue && !currentTileDoorResult.Value.CanMove)
            return new MoveResult(false, false, currentTileDoorResult.Value.DoorOpening);

        var canMoveResult = CanMoveToTile(uid, xform.GridUid.Value, grid, targetTile, direction, wishDir);
        if (!canMoveResult.CanMove)
        {
            if (canMoveResult.BlockedByHarmMode)
                return new MoveResult(false, true, false);
            
            return new MoveResult(false, canMoveResult.BlockedByWall, canMoveResult.DoorOpening);
        }

        if (!canMoveResult.SwappedPositions)
        {
            var targetCoords = _mapSystem.ToCenterCoordinates(xform.GridUid.Value, targetTile, grid);
            _transformSystem.SetLocalRotation(uid, wishDir.ToWorldAngle(), xform);
            _transformSystem.SetCoordinates(uid, xform, targetCoords);
        }

        return new MoveResult(true, false);
    }

    private CanMoveResult? CheckDoorsOnTile(EntityUid uid, EntityUid gridUid, MapGridComponent grid, Vector2i tilePos)
    {
        if (!_mapSystem.TryGetTileRef(gridUid, grid, tilePos, out var tileRef))
            return null;

        var entities = new HashSet<EntityUid>();
        _entityLookup.GetEntitiesInTile(tileRef, entities, LookupFlags.Dynamic | LookupFlags.Static);

        foreach (var otherEntity in entities)
        {
            if (otherEntity == uid || !TryComp<DoorComponent>(otherEntity, out var door))
                continue;

            if (!TryComp<TransformComponent>(otherEntity, out var doorXform))
                continue;

            var doorTile = _mapSystem.LocalToTile(gridUid, grid, doorXform.Coordinates);
            if (doorTile != tilePos)
                continue;

            var doorResult = GetDoorResult(otherEntity, door, uid);
            if (doorResult.HasValue)
                return doorResult;
        }

        return null;
    }

    private CanMoveResult? GetDoorResult(EntityUid doorEntity, DoorComponent door, EntityUid uid)
    {
        return door.State switch
        {
            DoorState.Open or DoorState.Opening => null,
            DoorState.Closing => new CanMoveResult(false, false, false),
            DoorState.Closed or DoorState.Denying => _doorSystem.TryOpen(doorEntity, door, uid, predicted: false)
                ? new CanMoveResult(false, false, true)
                : new CanMoveResult(false, false, false),
            _ => null
        };
    }

    private CanMoveResult CanMoveToTile(EntityUid uid, EntityUid gridUid, MapGridComponent grid, Vector2i tilePos, Vector2i? moveDirection = null, Vector2? wishDir = null)
    {
        if (!TryComp<PhysicsComponent>(uid, out var physics) || 
            !_mapSystem.TryGetTileRef(gridUid, grid, tilePos, out var tileRef))
            return new CanMoveResult(true, false, false);

        var entities = new HashSet<EntityUid>();
        _entityLookup.GetEntitiesInTile(tileRef, entities, LookupFlags.Dynamic | LookupFlags.Static);

        foreach (var otherEntity in entities)
        {
            if (otherEntity == uid)
                continue;

            if (!TryComp<TransformComponent>(otherEntity, out var otherXform))
                continue;

            var otherEntityTile = _mapSystem.LocalToTile(gridUid, grid, otherXform.Coordinates);
            if (otherEntityTile != tilePos)
                continue;

            if (HasComp<EntityTileMovementComponent>(otherEntity))
            {
                var moverHarmMode = _combatModeSystem.IsInCombatMode(uid);
                var otherHarmMode = _combatModeSystem.IsInCombatMode(otherEntity);

                if (otherHarmMode)
                    return new CanMoveResult(false, false, false, blockedByHarmMode: true);

                if (moverHarmMode && moveDirection.HasValue && wishDir.HasValue)
                {
                    var nextTile = tilePos + moveDirection.Value;
                    if (_mapSystem.TryGetTileRef(gridUid, grid, nextTile, out _))
                    {
                        var canPushResult = CanMoveToTile(otherEntity, gridUid, grid, nextTile);
                        if (canPushResult.CanMove && !canPushResult.BlockedByHarmMode)
                        {
                            var targetCoords = _mapSystem.ToCenterCoordinates(gridUid, tilePos, grid);
                            var nextTileCoords = _mapSystem.ToCenterCoordinates(gridUid, nextTile, grid);
                            
                            _transformSystem.SetLocalRotation(uid, wishDir.Value.ToWorldAngle(), Transform(uid));
                            _transformSystem.SetCoordinates(uid, Transform(uid), targetCoords);
                            
                            _transformSystem.SetLocalRotation(otherEntity, wishDir.Value.ToWorldAngle(), otherXform);
                            _transformSystem.SetCoordinates(otherEntity, otherXform, nextTileCoords);
                            
                            return new CanMoveResult(true, false, false, swappedPositions: true);
                        }
                    }
                    return new CanMoveResult(false, true, false);
                }

                if (moveDirection.HasValue && wishDir.HasValue)
                {
                    var moverXform = Transform(uid);
                    if (_transformSystem.SwapPositions((uid, moverXform), (otherEntity, otherXform)))
                    {
                        _transformSystem.SetLocalRotation(uid, wishDir.Value.ToWorldAngle(), moverXform);
                        _transformSystem.SetLocalRotation(otherEntity, (-wishDir.Value).ToWorldAngle(), otherXform);
                        return new CanMoveResult(true, false, false, swappedPositions: true);
                    }
                }
            }

            var doorResult = CheckDoorCollision(uid, otherEntity);
            if (doorResult.HasValue)
                return doorResult.Value;

            var physicsResult = CheckPhysicsCollision(uid, physics, otherEntity);
            if (physicsResult.HasValue)
                return physicsResult.Value;
        }

        return new CanMoveResult(true, false, false);
    }

    private CanMoveResult? CheckDoorCollision(EntityUid uid, EntityUid otherEntity)
    {
        if (!TryComp<DoorComponent>(otherEntity, out var door))
            return null;

        return GetDoorResult(otherEntity, door, uid);
    }

    private CanMoveResult? CheckPhysicsCollision(EntityUid uid, PhysicsComponent physics, EntityUid otherEntity)
    {
        if (!TryComp<PhysicsComponent>(otherEntity, out var otherPhysics) || !otherPhysics.CanCollide)
            return null;

        var hasCollision = (physics.CollisionMask & otherPhysics.CollisionLayer) != 0 ||
                          (otherPhysics.CollisionMask & physics.CollisionLayer) != 0;
        if (!hasCollision)
            return null;

        var collisionLayer = (CollisionGroup)otherPhysics.CollisionLayer;

        if ((collisionLayer & CollisionGroup.Impassable) != 0)
            return new CanMoveResult(false, true);

        if (otherPhysics.BodyType == BodyType.Dynamic || otherPhysics.BodyType == BodyType.KinematicController)
            return null;

        if ((collisionLayer & CollisionGroup.LowImpassable) != 0)
            return null;

        if ((collisionLayer & (CollisionGroup.MidImpassable | CollisionGroup.HighImpassable)) != 0)
            return new CanMoveResult(false, true);

        return new CanMoveResult(false, false, false);
    }

    private void StopMovement(EntityUid uid, InputMoverComponent mover)
    {
        if (!TryComp<PhysicsComponent>(uid, out var physics))
            return;

        _physicsSystem.SetLinearVelocity(uid, Vector2.Zero, wakeBody: false);
        _physicsSystem.SetAngularVelocity(uid, 0f, body: physics);
    }

    private struct MoveResult
    {
        public bool Moved;
        public bool BlockedByWall;
        public bool DoorOpening;

        public MoveResult(bool moved, bool blockedByWall, bool doorOpening = false)
        {
            Moved = moved;
            BlockedByWall = blockedByWall;
            DoorOpening = doorOpening;
        }
    }

    private struct CanMoveResult
    {
        public bool CanMove;
        public bool BlockedByWall;
        public bool DoorOpening;
        public bool BlockedByHarmMode;
        public bool SwappedPositions;

        public CanMoveResult(bool canMove, bool blockedByWall, bool doorOpening = false, bool blockedByHarmMode = false, bool swappedPositions = false)
        {
            CanMove = canMove;
            BlockedByWall = blockedByWall;
            DoorOpening = doorOpening;
            BlockedByHarmMode = blockedByHarmMode;
            SwappedPositions = swappedPositions;
        }
    }
}
