using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Content.Shared._CorvaxGoob.InternalResources.Data; // CorvaxGoob edit

namespace Content.Goobstation.Shared.InternalResources.Data;

/// <summary>
/// Data structure for storing and changing inner resource in entities
/// </summary>
[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class InternalResourcesData
{
    /// <summary>
    /// Current amount of resources
    /// </summary>
    [DataField]
    public float CurrentAmount = 0;

    /// <summary>
    /// Maximum amount of resources
    /// </summary>
    [DataField]
    public float MaxAmount = 100;

    /// <summary>
    /// Resources regeneration rate per update time
    /// </summary>
    [DataField]
    public float RegenerationRate = 1f;

    /// <summary>
    /// The thresholds at which InternalResourcesThresholdMetEvent will be raised.
    /// </summary>
    [DataField]
    public Dictionary<string, ThresholdData>? Thresholds; // CorvaxGoob edit

    /// <summary>
    /// Prototype with visual information of internal resources
    /// </summary>
    [DataField(required: true)]
    public ProtoId<InternalResourcesPrototype> InternalResourcesType;

    public InternalResourcesData(
        float maxAmount,
        float regenerationRate,
        float startingAmount,
        Dictionary<string, ThresholdData>? thresholds, // CorvaxGoob edit
        string protoId)
    {
        CurrentAmount = startingAmount;
        MaxAmount = maxAmount;
        RegenerationRate = regenerationRate;
        Thresholds = thresholds;
        InternalResourcesType = protoId;
    }
}
