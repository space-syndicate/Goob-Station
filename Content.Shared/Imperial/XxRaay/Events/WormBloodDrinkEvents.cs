using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.XxRaay.Events;

public sealed partial class WormBloodDrinkActionEvent : EntityTargetActionEvent;

[Serializable, NetSerializable]
public sealed partial class WormBloodDrinkAttachDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class WormBloodDrinkTickDoAfterEvent : SimpleDoAfterEvent;
