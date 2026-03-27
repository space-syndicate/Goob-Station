using Content.Server.Imperial.XxRaay.Systems;

namespace Content.Server.Imperial.XxRaay.Components;

/// <summary>
/// Marker component for supply pods created by orbital strike.
/// </summary>
[RegisterComponent, Access(typeof(OrbitalStrikeSystem))]
public sealed partial class OrbitalStrikePodComponent : Component
{
    /// <summary>
    /// Explosion intensity.
    /// </summary>
    [DataField]
    public float ExplosionIntensity = 100f;

    /// <summary>
    /// Explosion slope.
    /// </summary>
    [DataField]
    public float ExplosionSlope = 1f;

    /// <summary>
    /// Explosion max tile intensity.
    /// </summary>
    [DataField]
    public float ExplosionMaxTileIntensity = 12f;
}

