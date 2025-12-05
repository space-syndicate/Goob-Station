using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared.Imperial.Damage;

[NetworkedComponent, RegisterComponent]
public sealed partial class ImperialDamagedByCollideComponent : Component
{
    [ViewVariables]
    public DamageSpecifier? Damage;

    /// <summary>
    /// Will damage be dealt at regular intervals?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool? TimeDamage;

    /// <summary>
    /// The time that determines the amount of time after which damage will be dealt. Cyclic
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan? NextTimeDamage;

    /// <summary>
    /// How long does it take to finish dealing damage
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan? EndTimeDamage;

    [ViewVariables]
    public bool AddTimeEndDamage = true;

    [ViewVariables]
    public bool DamageOnce = true;

    [ViewVariables]
    public TimeSpan NextSecond = TimeSpan.Zero;

    [ViewVariables]
    public TimeSpan EndSecond = TimeSpan.Zero;
}
