using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Imperial.Lavaland.EmpoweredAttacks.Components;

[RegisterComponent]
public sealed partial class EnhancedBayonetAttackComponent : Component
{
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ActionEnhancedBayonetAttack = "ActionEnhancedBayonetAttack";

    [ViewVariables]
    public EntityUid? Action;

    [ViewVariables]
    public EntityUid? User;
}
