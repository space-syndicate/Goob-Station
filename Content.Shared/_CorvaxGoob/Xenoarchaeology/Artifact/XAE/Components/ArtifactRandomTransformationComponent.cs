using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared._CorvaxGoob.Xenoarchaeology.Artifact.XAE.Components;

[RegisterComponent]
public sealed partial class ArtifactRandomTransformationComponent : Component
{
    [DataField("radius")]
    public float Radius = 6.0f;

    [DataField("transformationPercentRatio")]
    public float TransformationPercentRatio = 0.35f;

    [DataField("prototypeIdBlacklistSubstrings")]
    public List<string> PrototypeIdBlacklistSubstrings = new();
}
// тес