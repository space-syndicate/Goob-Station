using Content.Shared.Damage;
using Content.Shared.FixedPoint;

namespace Content.Server.Zombies;

/// <summary>
/// Компонент, хранящий информацию о том,
/// какой урон будет наносится сущности после зомбификации от компонента BarotraumaComponent.
/// </summary>
[RegisterComponent]
public sealed partial class ZombieBarotraumaDamageComponent : Component
{
    [DataField]
    public DamageSpecifier Damage = new()
    {
        DamageDict = new() { { "Blunt", FixedPoint2.New(0.20) } }
    };
}
