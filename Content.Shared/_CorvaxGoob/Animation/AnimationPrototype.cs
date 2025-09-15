using Content.Shared._CorvaxGoob.Animation.API;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._CorvaxGoob.Animation;

[Prototype("animation")]
[Serializable, NetSerializable, DataDefinition]
public sealed partial class AnimationPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public List<AnimationTrackData> Tracks = new();

    [DataField(required: true)]
    public float Length = 0;

}
