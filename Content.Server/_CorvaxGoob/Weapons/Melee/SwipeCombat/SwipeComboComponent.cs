using Content.Shared._CorvaxGoob.Weapons.Melee.SwipeCombat;

namespace Content.Server._CorvaxGoob.Weapons.Melee.SwipeCombat;

/// <summary>
/// Стаки свайп-боевки, висящие на цели. Только сервер.
/// Удар другого типа, чем предыдущий, добавляет стак; на втором стаке срабатывает особый эффект.
/// Повтор того же удара начинает серию заново.
/// </summary>
[RegisterComponent]
public sealed partial class SwipeComboComponent : Component
{
    [ViewVariables]
    public EntityUid? Attacker;

    [ViewVariables]
    public SwipeAttackDirection? LastDirection;

    [ViewVariables]
    public int Stacks;

    [ViewVariables]
    public TimeSpan ExpiresAt;
}
