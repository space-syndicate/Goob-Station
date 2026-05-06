// CorvaxGoob
using Robust.Shared.Serialization;

namespace Content.Goobstation.Shared.InternalResources.Data;

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
