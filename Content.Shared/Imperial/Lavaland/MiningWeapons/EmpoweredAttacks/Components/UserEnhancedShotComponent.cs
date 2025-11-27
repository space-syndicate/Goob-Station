using System.Numerics;

namespace Content.Shared.Imperial.Lavaland.MiningWeapons.EmpoweredAttacks.Components;

[RegisterComponent]
public sealed partial class UserEnhancedShotComponent : Component
{
    [ViewVariables]
    public EntityUid? Item;

    [ViewVariables]
    public float DoAfterTime;

    [ViewVariables]
    public Vector2 Direction = Vector2.Zero;
}
