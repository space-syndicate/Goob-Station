using System.Numerics;
using Robust.Shared.Audio;

namespace Content.Shared.Imperial.Lavaland.MiningWeapons.EmpoweredAttacks.Components;

[RegisterComponent]
public sealed partial class UserEnhancedShotComponent : Component
{
    [ViewVariables, DataField]
    public SoundSpecifier? StartDoAfterSound;

    [ViewVariables]
    public EntityUid? Item;

    [ViewVariables]
    public float DoAfterTime;

    [ViewVariables]
    public Vector2 Direction = Vector2.Zero;
}
