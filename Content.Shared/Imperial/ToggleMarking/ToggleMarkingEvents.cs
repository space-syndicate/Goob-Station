using Content.Shared.Actions;

namespace Content.Shared.Imperial.ToggleMarking;

/// <summary>
/// Event triggered when the player uses the toggle marking action on ears.
/// </summary>
public sealed partial class ToggleMarkingEarsEvent : InstantActionEvent;

/// <summary>
/// Event triggered when the player uses the toggle marking action on the tail.
/// </summary>
public sealed partial class ToggleMarkingTailEvent : InstantActionEvent;
