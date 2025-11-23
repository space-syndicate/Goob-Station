using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Imperial.Lavaland.MiningWeapons.EmpoweredAttacks.Components;

[RegisterComponent]
public sealed partial class EnhancedShotComponent : Component
{
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ActionEnhancedShot = "ActionEnhancedShot";

    [ViewVariables]
    public EntityUid? Action;

    [ViewVariables]
    public EntityUid? User;

    [DataField, ViewVariables]
    public float DoAfterTime = 1.0f;

    [DataField]
    public string ProjectilePrototype = "EnhancedShotProjectile";

    [DataField]
    public float ProjectileSpeed = 40f;
}
