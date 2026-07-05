using System.Numerics;
using Robust.Shared.GameObjects;

namespace Content.Shared.Imperial.XxRaay.Events;

[ByRefEvent]
public record struct VentCrawlTileMoveAttemptEvent(
    EntityUid GridUid,
    Vector2i TargetTile,
    bool CanMove);
