using Robust.Shared.Prototypes;
using Content.Shared.Damage;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using System.Numerics;

namespace Content.Shared.Imperial.Lavaland.MiningWeapons.EmpoweredAttacks.Components;

[RegisterComponent]
public sealed partial class PiercingLungeComponent : Component
{
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ActionPiercingLunge = "ActionPiercingLunge";

    [ViewVariables]
    public EntityUid? Action;

    [ViewVariables]
    public EntityUid? User;

    [ViewVariables]
    public Vector2 Direction = Vector2.Zero;

    [ViewVariables]
    public bool IsInEffect = false;

    [DataField, ViewVariables]
    public DamageSpecifier Damage = new();

    [ViewVariables]
    public bool IsLunging = false;

    /// <summary>
    /// Only tracks sprint time
    /// </summary>
    [ViewVariables]
    public float LungeAccumulator = 0f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float DoAfterTime = 0.35f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float LungeStrength = 8f;

    /// <summary>
    /// Stun of the user during the dash and shortly after.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan StunTime = TimeSpan.FromSeconds(0.5f);

    /// <summary>
    /// Jerk momentum strength
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float InitialLungeStrength = 25f;

    /// <summary>
    /// Slowing down at the end of the dash
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float FinalLungeStrength = 25f;

    /// <summary>
    /// Snatch Distance
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float LungeDuration = 1.2f;
}
