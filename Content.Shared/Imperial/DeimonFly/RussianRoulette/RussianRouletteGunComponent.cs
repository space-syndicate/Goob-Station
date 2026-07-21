using Content.Shared.Damage;
using Robust.Shared.Prototypes;

namespace Content.Shared.Imperial.DeimonFly.RussianRoulette;

/// <summary>
/// Заставляет оружие наносить урон своему пользователю после каждого подтверждённого выстрела.
/// </summary>
[RegisterComponent]
public sealed partial class RussianRouletteGunComponent : Component
{
    /// <summary>
    /// Прототип единственного патрона, который помещается в барабан при создании револьвера.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Cartridge;

    /// <summary>
    /// Урон, напрямую наносимый стрелку. Система игнорирует сопротивления урону.
    /// </summary>
    [DataField(required: true)]
    public DamageSpecifier Damage = new();

    /// <summary>
    /// Добавляет стрелку <c>UnrevivableComponent</c> до нанесения урона.
    /// </summary>
    [DataField]
    public bool PreventRevival;
}
