using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Imperial.Lavaland.MiningWeapons.EmpoweredAttacks.Components;

/// <summary>
/// The time associated with the attack spawn, attack, and quick departure animations is not edited in the component.
/// </summary>
[RegisterComponent]
public sealed partial class EarthshakerStrikeComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float DoAfterTime = 1.5f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool HasDoAfter = true;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool HasWielded = true;


    [ViewVariables, DataField]
    public SoundSpecifier CompletedSound = new SoundPathSpecifier("/Audio/Effects/break_stone.ogg");

    [ViewVariables, DataField]
    public SoundSpecifier StartDoAfterSound = new SoundPathSpecifier("/Audio/Effects/falling.ogg");

    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>)), ViewVariables]
    public string EarthshakerRiftSpawnPrototype = "EffectEarthshakerRiftSpawn";

    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>)), ViewVariables]
    public string ActionEarthshakerStrike = "ActionEarthshakerStrike";


    [ViewVariables]
    public EntityUid? Action;

    [ViewVariables]
    public EntityUid? User;

}
