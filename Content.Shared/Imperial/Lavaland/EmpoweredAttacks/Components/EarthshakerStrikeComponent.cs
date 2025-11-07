using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Imperial.Lavaland.EmpoweredAttacks.Components;

[RegisterComponent]
public sealed partial class EarthshakerStrikeComponent : Component
{
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ActionEarthshakerStrike = "ActionEarthshakerStrike";

    [ViewVariables]
    public EntityUid? Action;

    [ViewVariables]
    public EntityUid? User;
}
