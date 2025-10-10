using System.Numerics;

namespace Content.Client.Imperial.ImperialLightning;


public record Lightning(
    (Vector2 StartCoords, EntityUid? StartEntityPoint) StartPoint,
    (Vector2 TargetCoords, EntityUid? TargetEntityPoint) TargetPoint,
    Vector3 LightningColor,
    Vector2 Offset,
    float Speed,
    float Intensity,
    float Seed,
    float Amplitude,
    float Frequency,
    TimeSpan DespawnTime
);
