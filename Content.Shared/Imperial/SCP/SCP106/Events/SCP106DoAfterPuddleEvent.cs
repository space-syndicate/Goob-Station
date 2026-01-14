using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.SCP.SCP106.Events;

[Serializable, NetSerializable]
public sealed partial class SCP106DoAfterPuddleEvent : SimpleDoAfterEvent
{
    public SCP106DoAfterPuddleEvent()
    {
    }
    public override DoAfterEvent Clone() => this;
}