using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using System.Collections.Generic;

namespace Content.Server.Xenoarchaeology.Artifact.XAE.Components;

[RegisterComponent]
public sealed partial class ArtifactRandomTransformationComponent : Component
{
    [DataField("radius")]
    public float Radius { get; set; } = 6.0f;

    [DataField("transformationPercentRatio")]
    public float TransformationPercentRatio { get; set; } = 0.25f;

    [DataField("prototypeIdBlacklistSubstrings")]
    public List<string> PrototypeIdBlacklistSubstrings { get; set; } = new()
    {
        "admin", "debug", "test", "singularity", "tesla", "board"
    };

    [DataField("prototypeSuffixBlacklistSubstrings")]
    public List<string> PrototypeSuffixBlacklistSubstrings { get; set; } = new()
    {
        "admin", "debug", "тест", "дебаг"
    };

    [DataField("componentBlacklist")]
    public List<string> ComponentBlacklist { get; set; } = new()
    {
        "Singularity", "SingularityGenerator", "TeslaEnergyBall"
    };
}