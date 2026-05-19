using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.Power;

[Serializable, NetSerializable]
public sealed partial class SupermatterShutdownDoAfterEvent : SimpleDoAfterEvent;
