using Robust.Shared.Serialization;

namespace Content.Shared._CorvaxGoob.InternalResources.Data;

[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class ThresholdData
{
    /// <summary>
    /// The threshold percentage from 0 to 1.
    /// </summary>
    [DataField]
    public float Percentage;

    /// <summary>
    /// Whether the threshold has been met.
    /// </summary>
    [DataField]
    public bool Met;
}
