using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Imperial.Lavaland.MiningWeapons.EmpoweredAttacks.EnhancedShot;

[RegisterComponent]
public sealed partial class EnhancedShotComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float DoAfterTime = 1.0f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float ProjectileSpeed = 40f;

    [ViewVariables, DataField]
    public SoundSpecifier CompletedSound = new SoundPathSpecifier("/Audio/Effects/break_stone.ogg");

    [ViewVariables, DataField]
    public SoundSpecifier StartDoAfterSound = new SoundPathSpecifier("/Audio/Effects/falling.ogg");

    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ActionEnhancedShot = "ActionEnhancedShot";

    [DataField, ViewVariables]
    public string ProjectilePrototype = "EnhancedShotProjectile";

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool HasDoAfter = true;

    [ViewVariables]
    public EntityUid? Action;

    [ViewVariables]
    public EntityUid? User;
}
