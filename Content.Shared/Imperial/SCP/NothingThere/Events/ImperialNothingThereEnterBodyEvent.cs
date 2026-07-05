using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.SCP.NothingThere.Events;

public sealed partial class ImperialNothingThereEnterBodyEvent : EntityTargetActionEvent;

[Serializable, NetSerializable]
public sealed partial class ImperialNothingThereEnterBodyDoAfterEvent : SimpleDoAfterEvent;