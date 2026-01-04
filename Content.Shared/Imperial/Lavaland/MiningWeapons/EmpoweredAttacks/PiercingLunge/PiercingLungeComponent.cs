using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using System.Numerics;

namespace Content.Shared.Imperial.Lavaland.MiningWeapons.EmpoweredAttacks.PiercingLunge;

[RegisterComponent]
public sealed partial class PiercingLungeComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool HasDoAfter = true;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float DoAfterTime = 0.35f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float LungeStrength = 7f;

    /// <summary>
    /// Stun of the user during the dash and shortly after.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan StunTime = TimeSpan.FromSeconds(0.5f);

    /// <summary>
    /// Jerk momentum strength
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float InitialLungeStrength = 30f;

    /// <summary>
    /// Slowing down at the end of the dash
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float FinalLungeStrength = 5f;

    /// <summary>
    /// Snatch Distance
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float LungeDuration = 0.5f;


    [ViewVariables, DataField]
    public SoundSpecifier CompletedSound = new SoundPathSpecifier("/Audio/Imperial/Lavaland/MiningWeapons/EmpoweredAttacks/piercing_lunge.ogg");

    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ActionPiercingLunge = "ActionPiercingLunge";


    [DataField, ViewVariables]
    public DamageSpecifier Damage = new()
    {
        DamageDict = new()
        {
            { "Blunt", 5 },
            { "Slash", 20 }
        }
    };

    /// <summary>
    /// Only tracks sprint time
    /// </summary>
    [ViewVariables]
    public float LungeAccumulator = 0f;

    [ViewVariables]
    public bool IsLunging = false;

    [ViewVariables]
    public EntityUid? Action;

    [ViewVariables]
    public EntityUid? User;

    [ViewVariables]
    public Vector2 Direction = Vector2.Zero;

    [ViewVariables]
    public bool IsInEffect = false;
}
