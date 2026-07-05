using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.XxRaay.Events;

public sealed partial class WormCorpseEnterActionEvent : EntityTargetActionEvent;

public sealed partial class WormCorpseExitActionEvent : InstantActionEvent;

[Serializable, NetSerializable]
public sealed partial class WormCorpseEnterDoAfterEvent : SimpleDoAfterEvent;
