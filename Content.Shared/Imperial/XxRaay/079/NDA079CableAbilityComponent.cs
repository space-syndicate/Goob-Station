using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Imperial.XxRaay.Nda079;

/// <summary>
/// Компонент для способности спавна кабеля NDA079
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class NDA079CableAbilityComponent : Component
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
    public float EnergyCost = 5f;

    /// <summary>
    /// Время перезарядки способности в секундах
    /// </summary>
    [DataField]
    public float CooldownSeconds = 0.3f;

    /// <summary>
    /// Прототип экшена для спавна кабеля
    /// </summary>
    [DataField]
    public EntProtoId ActionProto = "ActionNDA079SpawnCable";

    /// <summary>
    /// Прототип кабеля
    /// </summary>
    public EntProtoId CableProto = "Cable079";
}

