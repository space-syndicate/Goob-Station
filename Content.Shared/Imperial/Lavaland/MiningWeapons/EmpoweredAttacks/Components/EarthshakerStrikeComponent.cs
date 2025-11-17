using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Imperial.Lavaland.MiningWeapons.EmpoweredAttacks.Components;

/// <summary>
/// The time associated with the attack spawn, attack, and quick departure animations is not edited in the component.
/// </summary>
[RegisterComponent]
public sealed partial class EarthshakerStrikeComponent : Component
{
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string EarthshakerRiftSpawnPrototype = "EffectEarthshakerRiftSpawn";

    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ActionEarthshakerStrike = "ActionEarthshakerStrike";

    [ViewVariables]
    public EntityUid? Action;

    [ViewVariables]
    public EntityUid? User;

    [DataField, ViewVariables]
    public float DoAfterTime = 1.5f;

    [DataField, ViewVariables]
    public bool HasDoAfter = true;

    [DataField, ViewVariables]
    public bool HasWielded = true;
}
