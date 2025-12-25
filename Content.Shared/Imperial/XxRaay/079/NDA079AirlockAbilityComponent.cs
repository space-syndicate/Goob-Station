using Robust.Shared.GameStates;
using Robust.Shared.Timing;

namespace Content.Shared.Imperial.XxRaay.Nda079;

/// <summary>
/// Компонент для способности управления шлюзами NDA079
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class NDA079AirlockAbilityComponent : Component
{
    /// <summary>
    /// Время последнего использования способности
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan? LastUsedTime;

    /// <summary>
    /// Цена энергии за использование способности
    /// </summary>
    [DataField]
    public float EnergyCost = 15f;

    /// <summary>
    /// Время перезарядки способности в секундах
    /// </summary>
    [DataField]
    public float CooldownSeconds = 6f;

    /// <summary>
    /// Шанс успешного выполнения действия (0.0 - 1.0)
    /// </summary>
    [DataField]
    public float SuccessChance = 0.85f;

    /// <summary>
    /// Длительность заболтования в секундах
    /// </summary>
    [DataField]
    public float BoltDurationSeconds = 7f;
}
