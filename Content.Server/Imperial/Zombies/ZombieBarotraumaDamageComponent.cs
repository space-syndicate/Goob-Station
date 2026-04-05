using Content.Shared.Damage;
using Content.Shared.FixedPoint;

namespace Content.Shared.Imperial.Zombies;

[RegisterComponent]
public sealed partial class ZombieBarotraumaDamageComponent : Component
{
    /// <summary>
    /// Компонент, хранящий информацию о том,
    /// какой урон будет наносится сущности после зомбификации от компонента BarotraumaComponent.
    /// </summary>
    [DataField("damage")]
    public DamageSpecifier Damage = new()
    {
        DamageDict = new() { { "Blunt", FixedPoint2.New(0.20) } }
    };
}
