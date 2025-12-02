using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Audio;

namespace Content.Shared.Imperial.Lavaland.MiningWeapons.EmpoweredAttacks.Components;

[RegisterComponent]
public sealed partial class UserEarthshakerStrikeComponent : Component
{
    [ViewVariables, DataField]
    public SoundSpecifier? CompletedSound;

    [ViewVariables, DataField]
    public SoundSpecifier? StartDoAfterSound;

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
