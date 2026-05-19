using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.XxRaay.Components;

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

