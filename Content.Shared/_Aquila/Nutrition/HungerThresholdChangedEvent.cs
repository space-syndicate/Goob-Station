using Content.Shared.Nutrition.Components;

namespace Content.Shared._Aquila.Nutrition;

[ByRefEvent]
public readonly record struct HungerThresholdChangedEvent(HungerThreshold Previous, HungerThreshold Current);
