using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Imperial.MiningWeapons.EmpoweredAttacks.EnhancedShot;

[RegisterComponent]
public sealed partial class EnhancedShotComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float DoAfterTime = 1.5f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float ProjectileSpeed = 30f;

    [ViewVariables, DataField]
    public SoundSpecifier ShotSound = new SoundPathSpecifier("/Audio/Imperial/MiningWeapons/EmpoweredAttacks/EnhancedShot/enhanced_shot.ogg");

    [ViewVariables, DataField]
    public SoundSpecifier StartDoAfterSound = new SoundPathSpecifier("/Audio/Imperial/MiningWeapons/EmpoweredAttacks/EnhancedShot/charging_shot.ogg");

    [DataField, ViewVariables]
    public EntProtoId ActionEnhancedShot = "ActionEnhancedShot";

    [DataField, ViewVariables]
    public EntProtoId ProjectilePrototype = "EnhancedShotProjectile";

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool HasDoAfter = true;

    [ViewVariables]
    public EntityUid? Action;

    [ViewVariables]
    public EntityUid? User;
}
