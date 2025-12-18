namespace Content.Shared.Imperial.XxRaay.Types;

/// <summary>
/// Movement result for tile-based movement checks.
/// </summary>
public readonly record struct MoveResult(bool Moved, bool BlockedByWall, bool DoorOpening = false);

public readonly record struct CanMoveResult(
    bool CanMove,
    bool BlockedByWall,
    bool DoorOpening = false,
    bool BlockedByHarmMode = false,
    bool SwappedPositions = false);

