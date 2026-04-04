using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.Imperial.Zombies;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ZombieBarotraumaDamageComponent : Component
{
    /// <summary>
    /// The damage that the zombie will take from barotrauma.
    /// </summary>
    [DataField("damage"), AutoNetworkedField]
    public DamageSpecifier Damage = new()
    {
        DamageDict = new() { { "Blunt", FixedPoint2.New(0.20) } }
    };
}
