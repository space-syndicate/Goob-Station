using System.Numerics;
using Robust.Shared.Audio;

namespace Content.Shared.Imperial.Lavaland.MiningWeapons.EmpoweredAttacks.PiercingLunge;

[RegisterComponent]
public sealed partial class UserPiercingLungeComponent : Component
{
    [ViewVariables]
    public SoundSpecifier? StartDoAfterSound;

    [ViewVariables]
    public EntityUid? Item;

    [ViewVariables]
    public float DoAfterTime;

    [ViewVariables]
    public Vector2 Direction = Vector2.Zero;

    [ViewVariables]
    public bool HasDoAfter;
}
