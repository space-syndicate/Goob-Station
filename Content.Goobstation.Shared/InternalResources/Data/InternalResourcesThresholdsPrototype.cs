using Robust.Shared.Prototypes;
using Content.Shared._CorvaxGoob.InternalResources.Data; // CorvaxGoob edit

namespace Content.Goobstation.Shared.InternalResources.Data;

/// <summary>
/// Prototype for an internal resource's thresholds.
/// </summary>
[Prototype]
public sealed class InternalResourcesThresholdsPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Float is the percentage from 0 to 1.
    /// Bool is if the threshold was met.
    /// </summary>
    [DataField]
    public Dictionary<string, ThresholdData>? Thresholds; // CorvaxGoob edit
}
