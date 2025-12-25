using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Imperial.XxRaay.Nda079;

/// <summary>
/// Компонент для способности мерцания света NDA079
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class NDA079LightFlickerAbilityComponent : Component
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
    public float EnergyCost = 10f;

    /// <summary>
    /// Время перезарядки способности в секундах
    /// </summary>
    [DataField]
    public float CooldownSeconds = 10f;

    /// <summary>
    /// Шанс успешного выполнения действия (0.0 - 1.0)
    /// </summary>
    [DataField]
    public float SuccessChance = 0.9f;

    /// <summary>
    /// Радиус действия способности в тайлах
    /// </summary>
    [DataField]
    public float Radius = 10f;

    /// <summary>
    /// Длительность отключения света при успехе в секундах
    /// </summary>
    [DataField]
    public float LightOffDurationSeconds = 5f;

    /// <summary>
    /// Длительность короткого мигания при провале в секундах
    /// </summary>
    [DataField]
    public float FlickerDurationSeconds = 1f;

    /// <summary>
    /// Прототип экшена для мерцания света
    /// </summary>
    [DataField]
    public EntProtoId ActionProto = "ActionNDA079LightFlicker";
}

