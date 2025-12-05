using Content.Shared.Damage;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.Imperial.Damage;

[NetworkedComponent, RegisterComponent]
public sealed partial class ImperialDamageOnCollideComponent : Component
{
    /// <summary>
    /// The damage done each second to those touching this entity
    /// </summary>
    [DataField(required: true)]
    public DamageSpecifier Damage = new();

    /// <summary>
    /// Entities that aren't damaged by this entity
    /// </summary>
    [DataField]
    public EntityWhitelist? IgnoreWhitelist;

    /// <summary>
    /// Will damage be dealt at regular intervals?
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool TimeDamage = false;

    /// <summary>
    /// The time that determines the amount of time after which damage will be dealt. Cyclic
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan? NextTimeDamage = TimeSpan.FromSeconds(1f);

    /// <summary>
    /// How long does it take to finish dealing damage
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan? EndTimeDamage = TimeSpan.FromSeconds(2f);
}
