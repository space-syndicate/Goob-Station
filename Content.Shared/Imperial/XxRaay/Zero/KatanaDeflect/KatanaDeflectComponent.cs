using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Imperial.XxRaay.Zero.KatanaDeflect;

/// <summary>
/// Component that allows a katana to deflect projectiles during attack windows.
/// Inspired by Katana ZERO mechanics.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class KatanaDeflectComponent : Component
{
    /// <summary>
    /// Duration of the deflection window after an attack, in seconds.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ActiveWindow = 0.8f;

    /// <summary>
    /// Radius around the wielder to detect projectiles for deflection.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Radius = 3f;

    /// <summary>
    /// Angle of the deflection cone in front of the attack direction.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DeflectAngle = 120f;

    /// <summary>
    /// Time when the last attack occurred, used to track active deflection window.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan? LastAttackTime;

    /// <summary>
    /// Direction of the last attack swing, used for deflection cone calculation.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Vector2? LastAttackDirection;

    /// <summary>
    /// Whether a projectile has already been deflected in this attack window.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool HasDeflected = false;
}
