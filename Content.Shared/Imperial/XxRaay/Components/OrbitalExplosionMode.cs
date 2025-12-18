using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.XxRaay.Components;

[DataDefinition]
public sealed partial class OrbitalExplosionMode
{
    [DataField]
    public float Intensity { get; set; }

    [DataField]
    public float Slope { get; set; }

    [DataField]
    public float MaxTileIntensity { get; set; }

    public OrbitalExplosionMode() { }

    public OrbitalExplosionMode(float intensity, float slope, float maxTileIntensity)
    {
        Intensity = intensity;
        Slope = slope;
        MaxTileIntensity = maxTileIntensity;
    }
}

