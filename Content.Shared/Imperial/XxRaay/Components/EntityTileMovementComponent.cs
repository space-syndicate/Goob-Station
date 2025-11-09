using System.Numerics;
using Content.Shared.Imperial.XxRaay.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.XxRaay.Components;

/// <summary>
/// Компонент для потайлового движения
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedEntityTileMovementSystem))]
public sealed partial class EntityTileMovementComponent : Component
{
    /// <summary>
    /// Скорость перемещения между тайлами
    /// </summary>
    [DataField]
    public float MoveDelay = 0.1f;

    /// <summary>
    /// Время последнего перемещения.
    /// </summary>
    public TimeSpan LastMoveTime = TimeSpan.Zero;

    /// <summary>
    /// Направление последнего запроса на движение.
    /// </summary>
    public Vector2i? PendingMoveDirection;

    /// <summary>
    /// Последние нажатые кнопки движения.
    /// </summary>
    public MoveButtons LastMoveButtons = MoveButtons.None;

    /// <summary>
    /// Целевой тайл, к которому движется сущность.
    /// </summary>
    public Vector2i? TargetTile;

    /// <summary>
    /// Включено ли потайловое движение.
    /// </summary>
    [DataField]
    public bool Enabled = true;
}

/// <summary>
/// Состояние компонента для синхронизации между клиентом и сервером.
/// </summary>
[Serializable, NetSerializable]
public sealed class EntityTileMovementComponentState : ComponentState
{
    public float MoveDelay;
    public bool Enabled;

    public EntityTileMovementComponentState(float moveDelay, bool enabled)
    {
        MoveDelay = moveDelay;
        Enabled = enabled;
    }
}

