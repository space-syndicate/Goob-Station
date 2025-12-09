using System;
using System.Collections.Generic;
using Robust.Shared.GameStates;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;

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
    /// Interval between pod spawns.
    /// </summary>
    [DataField]
    public TimeSpan SpawnInterval = TimeSpan.FromSeconds(1.25f);

    /// <summary>
    /// Available pod counts that can be selected.
    /// </summary>
    [DataField]
    public List<int> AvailablePodCounts = new() { 3, 6, 8, 15, 20, 25, 30, 40, 60, 100 };

    /// <summary>
    /// Current selected pod count.
    /// </summary>
    [DataField]
    public int CurrentPodCount = 6;

    /// <summary>
    /// Available radius values that can be selected.
    /// </summary>
    [DataField]
    public List<float> AvailableRadii = new() { 15f, 20f, 30f, 40f, 50f, 60f, 100f };

    /// <summary>
    /// Current selected radius.
    /// </summary>
    [DataField]
    public float CurrentRadius = 30f;

    [DataField]
    public EntProtoId PodPrototype = "orbital_strike_pod_spawn";

    [DataField]
    public LocId PopupLaunchLoc = new("orbital-strike-popup-launch");

    [DataField]
    public LocId VerbCountLoc = new("orbital-strike-verb-count");

    [DataField]
    public LocId VerbRadiusLoc = new("orbital-strike-verb-radius");

    [DataField]
    public LocId VerbModeLoc = new("orbital-strike-verb-mode");

    /// <summary>
    /// Available explosion modes.
    /// </summary>
    [DataField]
    public Dictionary<LocId, OrbitalExplosionMode> AvailableExplosionModes = new()
    {
        { new LocId("orbital-strike-mode-weak"), new OrbitalExplosionMode(70f, 1f, 7f) },
        { new LocId("orbital-strike-mode-medium"), new OrbitalExplosionMode(120f, 1f, 12f) },
        { new LocId("orbital-strike-mode-strong"), new OrbitalExplosionMode(160f, 3f, 100f) }
    };

    /// <summary>
    /// Current selected explosion mode name.
    /// </summary>
    [DataField]
    public LocId CurrentExplosionMode = new("orbital-strike-mode-medium");
}

