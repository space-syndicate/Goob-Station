using Robust.Shared.GameStates;

namespace Content.Shared.Imperial.XxRaay.Components;

/// <summary>
/// Component for orbital strike item that spawns supply pods in a radius.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class OrbitalStrikeComponent : Component
{
    /// <summary>
    /// Radius in tiles for spawning pods.
    /// </summary>
    [DataField]
    public float Radius = 30f;

    /// <summary>
    /// Interval between pod spawns in seconds.
    /// </summary>
    [DataField]
    public float SpawnInterval = 1.25f;

    /// <summary>
    /// Available pod counts that can be selected.
    /// </summary>
    [DataField]
    public List<int> AvailablePodCounts = new() { 3, 6, 8, 15, 20, 25, 30, 40 };

    /// <summary>
    /// Current selected pod count.
    /// </summary>
    [DataField]
    public int CurrentPodCount = 6;

    /// <summary>
    /// Available radius values that can be selected.
    /// </summary>
    [DataField]
    public List<float> AvailableRadii = new() { 15f, 20f, 30f, 40f, 50f };

    /// <summary>
    /// Current selected radius.
    /// </summary>
    [DataField]
    public float CurrentRadius = 30f;

    /// <summary>
    /// Explosion mode: (intensity, slope, maxTileIntensity)
    /// </summary>
    [DataDefinition]
    public sealed partial class ExplosionMode
    {
        [DataField("intensity")]
        public float Intensity { get; set; }

        [DataField("slope")]
        public float Slope { get; set; }

        [DataField("maxTileIntensity")]
        public float MaxTileIntensity { get; set; }

        public ExplosionMode() { }

        public ExplosionMode(float intensity, float slope, float maxTileIntensity)
        {
            Intensity = intensity;
            Slope = slope;
            MaxTileIntensity = maxTileIntensity;
        }
    }

    /// <summary>
    /// Available explosion modes.
    /// </summary>
    [DataField]
    public Dictionary<string, ExplosionMode> AvailableExplosionModes = new()
    {
        { "Слабый", new ExplosionMode(70f, 1f, 7f) },
        { "Средний", new ExplosionMode(120f, 1f, 12f) },
        { "Сильный", new ExplosionMode(160f, 3f, 100f) }
    };

    /// <summary>
    /// Current selected explosion mode name.
    /// </summary>
    [DataField]
    public string CurrentExplosionMode = "Средний";
}

