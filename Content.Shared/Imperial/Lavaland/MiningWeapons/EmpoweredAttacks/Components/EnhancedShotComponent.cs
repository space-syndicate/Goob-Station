using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Audio;

namespace Content.Shared.Imperial.Lavaland.MiningWeapons.EmpoweredAttacks.Components;

[RegisterComponent]
public sealed partial class EnhancedShotComponent : Component
{
    [ViewVariables, DataField]
    public SoundSpecifier CompletedSound = new SoundPathSpecifier("/Audio/Effects/break_stone.ogg");

    [ViewVariables, DataField]
    public SoundSpecifier StartDoAfterSound = new SoundPathSpecifier("/Audio/Effects/falling.ogg");

    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ActionEnhancedShot = "ActionEnhancedShot";

    [ViewVariables]
    public EntityUid? Action;

    [ViewVariables]
    public EntityUid? User;

    [DataField, ViewVariables]
    public string ProjectilePrototype = "EnhancedShotProjectile";

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float DoAfterTime = 1.0f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float ProjectileSpeed = 40f;
}
