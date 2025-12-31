using Content.Shared.DoAfter;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.Vampire
{
    [Serializable, NetSerializable]
    public sealed partial class VampireShadowTrapDoAfterEvent : SimpleDoAfterEvent
    {
        public override DoAfterEvent Clone() => this;

        [DataField("targetCoords")]
        public NetCoordinates TargetCoords;
    }

    [Serializable, NetSerializable]
    public sealed partial class VampireCureGhoulDoAfterEvent : DoAfterEvent
    {
        public override DoAfterEvent Clone() => this;
    }

    [Serializable, NetSerializable]
    public sealed partial class VampireDrinkingDoAfterEvent : DoAfterEvent
    {
        public override DoAfterEvent Clone() => this;
    }

    [Serializable, NetSerializable]
    public sealed partial class VampireEnvelopeDoAfterEvent : DoAfterEvent
    {
        public override DoAfterEvent Clone() => this;
    }

    [Serializable, NetSerializable]
    public sealed partial class VampireReconciliationDoAfterEvent : DoAfterEvent
    {
        public override DoAfterEvent Clone() => this;
    }

    [Serializable, NetSerializable]
    public sealed partial class VampireSleepDoAfterEvent : DoAfterEvent
    {
        public override DoAfterEvent Clone() => this;
    }
}
