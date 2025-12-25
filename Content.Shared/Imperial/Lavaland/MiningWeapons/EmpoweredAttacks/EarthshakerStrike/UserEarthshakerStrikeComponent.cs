using Robust.Shared.Audio;

namespace Content.Shared.Imperial.Lavaland.MiningWeapons.EmpoweredAttacks.EarthshakerStrike;

[RegisterComponent]
public sealed partial class UserEarthshakerStrikeComponent : Component
{
    [ViewVariables]
    public SoundSpecifier? CompletedSound;

    [ViewVariables]
    public SoundSpecifier? StartDoAfterSound;

    [ViewVariables]
    public EntityUid? Item;

    [ViewVariables]
    public float DoAfterTime;

    [ViewVariables]
    public string EarthshakerRiftSpawnPrototype;

    [ViewVariables]
    public bool HasDoAfter;

    [ViewVariables]
    public bool HasWielded;
}
