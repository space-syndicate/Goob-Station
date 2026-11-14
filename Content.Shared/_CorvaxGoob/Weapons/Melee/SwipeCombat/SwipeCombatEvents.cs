using System.Numerics;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._CorvaxGoob.Weapons.Melee.SwipeCombat;

/// <summary>
/// Направление «свайпа» мышью при зажатой ЛКМ относительно того, куда смотрит персонаж.
/// </summary>
[Serializable, NetSerializable]
public enum SwipeAttackDirection : byte
{
    /// <summary>Сверху вниз (мышь к себе): длинный узкий удар, притягивает цель, без урона.</summary>
    Overhead,

    /// <summary>Снизу вверх (мышь от себя): короткий узкий тычок, x1.5 урона.</summary>
    Thrust,

    /// <summary>Справа налево: размашистый удар по нескольким целям.</summary>
    SlashLeft,

    /// <summary>Слева направо: размашистый удар по нескольким целям.</summary>
    SlashRight,
}

/// <summary>
/// Клиент -> сервер: игрок отпустил ЛКМ после свайпа оружием со <see cref="SwipeCombatComponent"/>.
/// </summary>
[Serializable, NetSerializable]
public sealed class SwipeAttackRequestEvent : EntityEventArgs
{
    public NetEntity Weapon;
    public SwipeAttackDirection Direction;

    /// <summary>Точка прицеливания: на 1 тайл перед персонажем.</summary>
    public NetCoordinates Coordinates;

    public SwipeAttackRequestEvent(NetEntity weapon, SwipeAttackDirection direction, NetCoordinates coordinates)
    {
        Weapon = weapon;
        Direction = direction;
        Coordinates = coordinates;
    }
}

/// <summary>
/// Сервер -> клиенты: проиграть анимацию свайп-удара.
/// </summary>
[Serializable, NetSerializable]
public sealed class SwipeAttackVisualEvent : EntityEventArgs
{
    public NetEntity User;
    public NetEntity Weapon;
    public SwipeAttackDirection Direction;

    /// <summary>Направление удара в системе координат родителя атакующего (как у обычного lunge).</summary>
    public Vector2 LocalDirection;

    public float Range;
    public Angle Arc;

    /// <summary>Удар со стаками — рисуем монтировку с синим контуром.</summary>
    public bool Special;

    public SwipeAttackVisualEvent(NetEntity user, NetEntity weapon, SwipeAttackDirection direction,
        Vector2 localDirection, float range, Angle arc, bool special)
    {
        User = user;
        Weapon = weapon;
        Direction = direction;
        LocalDirection = localDirection;
        Range = range;
        Arc = arc;
        Special = special;
    }
}
