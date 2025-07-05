using Robust.Shared.GameStates;

namespace Content.Shared.Imperial.DecalGun;

/// <summary>
/// Stores the current placement configuration for a decal gun.
/// This includes the selected decal ID, color tint, snap-to-tile toggle,
/// rotation in degrees, and whether the decal is cleanable.
/// </summary>
[RegisterComponent]
[NetworkedComponent]
public sealed partial class DecalGunComponent : Component
{
    /// <summary>
    /// The prototype ID of the selected decal to be placed.
    /// </summary>
    [ViewVariables]
    public string ChosenDecal;

    /// <summary>
    /// The color the decal will be tinted with.
    /// </summary>
    [ViewVariables]
    public Color ChosenColor;

    /// <summary>
    /// Whether the decal should snap to tile centers.
    /// </summary>
    [ViewVariables]
    public bool IsSnap;

    /// <summary>
    /// Rotation of the decal in degrees.
    /// </summary>
    [ViewVariables]
    public float Rotation;

    /// <summary>
    /// Whether the decal is cleanable.
    /// </summary>
    [ViewVariables]
    public bool IsCleanable;
}
