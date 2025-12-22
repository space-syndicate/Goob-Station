using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.Vampire
{
    [Serializable, NetSerializable]
    public sealed partial class VampireReconciliationDoAfterEvent : DoAfterEvent
    {
        public override DoAfterEvent Clone() => this;
    }
}
