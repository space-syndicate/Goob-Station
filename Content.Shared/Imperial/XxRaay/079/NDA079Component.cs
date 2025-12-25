using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Imperial.XxRaay.Nda079;

/// <summary>
/// Компонент для NDA079
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class NDA079Component : Component
{
    /// <summary>
    /// Находится ли сущность в режиме зрения ИИ
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool InAIVisionMode = false;

    /// <summary>
    /// Ссылка на оригинальную сущность
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? OriginalEntity;

    /// <summary>
    /// Ссылка на летающую сущность в режиме ИИ
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? AIVisionEntity;

    /// <summary>
    /// Прототип летающей сущности для режима зрения ИИ
    /// </summary>
    [DataField]
    public EntProtoId AIVisionFlyingEntityProto = "NDA079AIVisionEntity";

    /// <summary>
    /// Прототип оригинальной сущности (для восстановления)
    /// </summary>
    [DataField]
    public EntProtoId? OriginalEntityProto;

    /// <summary>
    /// Прототип экшена для переключения режима
    /// </summary>
    [DataField]
    public EntProtoId ToggleActionProto = "ActionNDA079ToggleVisionMode";

    /// <summary>
    /// Время последнего использования способности мерцания света 
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan? LightFlickerLastUsedTime;

    /// <summary>
    /// Время последнего использования способности управления шлюзами
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan? AirlockAbilityLastUsedTime;
}
