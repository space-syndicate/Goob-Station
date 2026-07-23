using Robust.Shared.Prototypes;

namespace Content.Server.Imperial.Other.TFG.Toughguys;

/// <summary>
/// Настройки случайного наполнения EntityStorage уникальными плюшевыми персонажами.
/// </summary>
[RegisterComponent]
public sealed partial class UniqueRandomPlushieFillComponent : Component
{
    /// <summary>
    /// Редкие варианты игрушек. Для Gift-версий обычный прототип считается тем же персонажем.
    /// </summary>
    [DataField]
    public List<EntProtoId> RarePrototypes = new();

    /// <summary>
    /// Шанс выбрать редкую игрушку на каждой попытке.
    /// </summary>
    [DataField]
    public float RareChance = 0.15f;

    /// <summary>
    /// Основной список игрушек.
    /// </summary>
    [DataField]
    public List<EntProtoId> Prototypes = new();

    /// <summary>
    /// Шанс выбрать обычную игрушку, если редкая не выпала.
    /// </summary>
    [DataField]
    public float Chance = 0.75f;

    /// <summary>
    /// Максимальное число попыток выбора уникальной игрушки.
    /// Неудачная проверка Chance оставляет соответствующую попытку пустой.
    /// </summary>
    [DataField]
    public int Amount = 25;
}
