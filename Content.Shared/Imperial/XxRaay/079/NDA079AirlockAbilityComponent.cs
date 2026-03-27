using Robust.Shared.GameStates;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

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
    [DataField]
    public TimeSpan? LastUsedTime;

    /// <summary>
    /// Цена энергии за использование способности
    /// </summary>
    [DataField, AutoNetworkedField]
    public float EnergyCost = 15f;

    /// <summary>
    /// Время перезарядки способности
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(6);

    /// <summary>
    /// Шанс успешного выполнения действия (0.0 - 1.0)
    /// </summary>
    [DataField, AutoNetworkedField]
    public float SuccessChance = 0.85f;

    /// <summary>
    /// Длительность заболтования
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan BoltDuration = TimeSpan.FromSeconds(7);

    /// <summary>
    /// Путь к RSI ресурсу с иконками для UI
    /// </summary>
    [DataField, AutoNetworkedField]
    public ResPath DoorsRsiPath = new("/Textures/Imperial/XxRaay/079/doors.rsi");

    /// <summary>
    /// Задержка между открытиями UI для предотвращения спама
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan UiCooldown = TimeSpan.FromSeconds(0.5);
}
