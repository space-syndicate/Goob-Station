using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.XxRaay.Events;

public sealed partial class WormEvolutionActionEvent : InstantActionEvent;

public sealed partial class WormCocoonObserveActionEvent : InstantActionEvent;

[Serializable, NetSerializable]
public sealed partial class WormEvolutionDoAfterEvent : SimpleDoAfterEvent;
