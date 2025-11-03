using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.Medical
{
    [Serializable, NetSerializable]
    public sealed partial class DoAfterCustomHyposprayEvent : DoAfterEvent
    {
        public override DoAfterEvent Clone() => this;
    }
}
