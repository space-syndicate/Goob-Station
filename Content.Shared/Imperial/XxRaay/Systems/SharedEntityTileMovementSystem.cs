using System.Numerics;
using Content.Shared.Imperial.XxRaay.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.XxRaay.Systems;

/// <summary>
/// Система для потайлового движения.
/// </summary>
public abstract class SharedEntityTileMovementSystem : EntitySystem
{
    [Dependency] protected readonly MovementSpeedModifierSystem MovementSpeed = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EntityTileMovementComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<EntityTileMovementComponent, ComponentHandleState>(OnHandleState);
    }

    private void OnGetState(EntityUid uid, EntityTileMovementComponent component, ref ComponentGetState args)
    {
        args.State = new EntityTileMovementComponentState(
            component.MoveDelay,
            component.Enabled);
    }

    private void OnHandleState(EntityUid uid, EntityTileMovementComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not EntityTileMovementComponentState state)
            return;

        component.MoveDelay = state.MoveDelay;
        component.Enabled = state.Enabled;
    }

    protected Vector2 GetDirectionFromButtons(MoveButtons buttons)
    {
        var x = 0;
        x -= (buttons & MoveButtons.Left) != 0 ? 1 : 0;
        x += (buttons & MoveButtons.Right) != 0 ? 1 : 0;

        var y = 0;
        y -= (buttons & MoveButtons.Down) != 0 ? 1 : 0;
        y += (buttons & MoveButtons.Up) != 0 ? 1 : 0;

        var vec = new Vector2(x, y);
        return vec.LengthSquared() > 0 ? vec.Normalized() : Vector2.Zero;
    }

    protected Vector2i GetTileDirection(Vector2 wishDir)
    {
        if (wishDir.LengthSquared() < 0.1f)
            return Vector2i.Zero;

        var normalized = wishDir.Normalized();
        var absX = Math.Abs(normalized.X);
        var absY = Math.Abs(normalized.Y);

        if (absX > absY)
            return normalized.X > 0 ? new Vector2i(1, 0) : new Vector2i(-1, 0);
        
        if (absY > absX)
            return normalized.Y > 0 ? new Vector2i(0, 1) : new Vector2i(0, -1);
        
        var xDir = normalized.X > 0 ? 1 : (normalized.X < 0 ? -1 : 0);
        var yDir = normalized.Y > 0 ? 1 : (normalized.Y < 0 ? -1 : 0);
        return new Vector2i(xDir, yDir);
    }

    protected float GetMoveSpeed(EntityUid uid, bool isWalking)
    {
        if (!TryComp<MovementSpeedModifierComponent>(uid, out var speedModifier))
            return isWalking ? 2.5f : 4.5f;

        return isWalking ? speedModifier.CurrentWalkSpeed : speedModifier.CurrentSprintSpeed;
    }
}

