using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.PiratesNewHorizon.GPS.Events;

[Serializable, NetSerializable]
public sealed partial class GPSTrackerRemoveDoAfterEvent : DoAfterEvent
{
    public GPSTrackerRemoveDoAfterEvent()
    {
    }
    public override DoAfterEvent Clone() => this;
}