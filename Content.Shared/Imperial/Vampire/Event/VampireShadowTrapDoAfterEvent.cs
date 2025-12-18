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
}
