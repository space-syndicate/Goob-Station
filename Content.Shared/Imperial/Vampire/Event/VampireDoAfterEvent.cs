using Content.Shared.DoAfter;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.Vampire
{
    [Serializable, NetSerializable]
    public sealed partial class VampireShadowTrapDoAfterEvent : SimpleDoAfterEvent
    {
        public override DoAfterEvent Clone() => this;

        [DataField]
        public NetCoordinates TargetCoords;

        [DataField]
        public string VampireTrapID;
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

        [DataField]
        public float StaminaDamage;

        [DataField]
        public float DamageItem;

        [DataField]
        public TimeSpan KnockdownTime;

        [DataField]
        public string DamageType;
    }

    [Serializable, NetSerializable]
    public sealed partial class VampireSleepDoAfterEvent : DoAfterEvent
    {
        public override DoAfterEvent Clone() => this;

        [DataField]
        public TimeSpan SleepingTime;
    }

    [Serializable, NetSerializable]
    public sealed partial class VampireAnchorCreateDoAfterEvent : DoAfterEvent
    {
        public override DoAfterEvent Clone() => this;

        [DataField]
        public TimeSpan Duration;

        [DataField]
        public string AnchorId;
    }
}
