using Content.Shared.Chemistry.Components;

namespace Content.Shared.Imperial.DeimonFly.BuckshotRoulette;

/// <summary>
/// Выдаёт выпившему пустую бутылку одно разрешение на извлечение текущего патрона.
/// </summary>
[RegisterComponent]
public sealed partial class BuckshotRouletteBeerComponent : Component
{
    [DataField]
    public string SolutionName = "drink";

    /// <summary>
    /// Кэш сущности раствора нужен только для штатного API химии.
    /// </summary>
    [ViewVariables]
    public Entity<SolutionComponent>? Solution;

    /// <summary>
    /// Не позволяет одной бутылке выдать разрешение повторно после доливания жидкости.
    /// </summary>
    [DataField]
    public bool RewardGranted;
}
