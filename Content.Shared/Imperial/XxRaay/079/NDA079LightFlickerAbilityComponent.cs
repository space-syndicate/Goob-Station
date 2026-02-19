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
    [DataField]
    public TimeSpan? LastUsedTime;

    /// <summary>
    /// Цена энергии за использование способности
    /// </summary>
    [DataField, AutoNetworkedField]
    public float EnergyCost = 10f;

    /// <summary>
    /// Время перезарядки способности
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Шанс успешного выполнения действия (0.0 - 1.0)
    /// </summary>
    [DataField, AutoNetworkedField]
    public float SuccessChance = 0.9f;

    /// <summary>
    /// Радиус действия способности в тайлах
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Radius = 10f;

    /// <summary>
    /// Длительность отключения света при успехе
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan LightOffDuration = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Длительность короткого мигания при провале
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan FlickerDuration = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Прототип экшена для мерцания света
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId ActionProto = "ActionNDA079LightFlicker";
}

