using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Imperial.Lavaland.MiningWeapons.EmpoweredAttacks.Components;

[RegisterComponent]
public sealed partial class UserEarthshakerStrikeComponent : Component
{
    [ViewVariables]
    public EntityUid? Item;

    [ViewVariables]
    public float DoAfterTime;

    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string EarthshakerRiftSpawnPrototype;

    [DataField, ViewVariables]
    public bool HasDoAfter;

    [DataField, ViewVariables]
    public bool HasWielded;
}
