using Robust.Shared.Animations;

namespace Content.Shared._CorvaxGoob.Animation.API;

[Serializable, DataDefinition]
public sealed partial class AnimationTrackData
{
    [DataField]
    [AlwaysPushInheritance]
    public string ComponentType;

    [DataField]
    [AlwaysPushInheritance]
    public string Property;

    [DataField]
    [AlwaysPushInheritance]
    public AnimationInterpolationMode InterpolationMode;

    [DataField]
    [AlwaysPushInheritance]
    public List<KeyFrameData> KeyFrames;
}

[Serializable, DataDefinition]
public sealed partial class KeyFrameData
{
    [DataField]
    [AlwaysPushInheritance]
    public string Value;

    [DataField]
    [AlwaysPushInheritance]
    public string Type;

    [DataField]
    [AlwaysPushInheritance]
    public float Keyframe;
}
