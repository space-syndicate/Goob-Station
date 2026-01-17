using System.Numerics;
using Content.Shared.Damage;
using Robust.Shared.Audio;

namespace Content.Shared.Imperial.MiningWeapons.EmpoweredAttacks.PiercingLunge;

[RegisterComponent]
public sealed partial class UserPiercingLungeComponent : Component
{
    [ViewVariables]
    public EntityUid? Item;

    [ViewVariables]
    public TimeSpan DoAfterTime;

    [ViewVariables]
    public Angle Direction = Angle.Zero;

    [ViewVariables]
    public bool HasDoAfter;

    [ViewVariables(VVAccess.ReadWrite)]
    public bool CanDamage = false;

    [ViewVariables]
    public DamageSpecifier? Damage = new();
}
