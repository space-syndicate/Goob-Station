using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._CorvaxGoob.Damage.Components;

/// <summary>
/// Добавляется к прототипу снаряда. Сносит стамину в зависимости от полученного определённого типа урона с учётом защиты.
/// </summary>
[RegisterComponent]
public sealed partial class StaminaDamageModifierOnCollideComponent : Component
{
    /// <summary>
    /// Коэф., умножающий финальный результат просчётов модификаторов.
    /// </summary>
    [DataField]
    public float StaminaCoefficient { get; set; } = 1f;

    /// <summary>
    /// Используемый модификатор на основе которого будет высчитываться стаминаурон.
    /// </summary>
    [DataField]
    public ProtoId<DamageTypePrototype>? AppliedModifier = "Blunt";
}
