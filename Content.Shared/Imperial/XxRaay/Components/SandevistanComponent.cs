using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Imperial.XxRaay.Components;

/// <summary>
/// Компонент для управления эффектом сандевистана.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class SandevistanComponent : Component
{
    /// <summary>
    /// Время окончания эффекта сандевистана.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField]
    [AutoPausedField]
    public TimeSpan? EffectEndTime;

    /// <summary>
    /// Время начала перезарядки.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField]
    [AutoPausedField]
    public TimeSpan? CooldownStartTime;

    /// <summary>
    /// Время окончания перезарядки.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField]
    [AutoPausedField]
    public TimeSpan? CooldownEndTime;

    /// <summary>
    /// Прототип экшена сандевистана.
    /// </summary>
    [DataField]
    public EntProtoId ActionPrototype = "ActionSandevistan";

    /// <summary>
    /// Сущность экшена сандевистана.
    /// </summary>
    [ViewVariables]
    public EntityUid? ActionEntity;

    /// <summary>
    /// Длительность
    /// </summary>
    [DataField]
    public float EffectDuration = 8f;

    /// <summary>
    /// Кулдаун
    /// </summary>
    [DataField]
    public float CooldownDuration = 60f;

    /// <summary>
    /// Бонус к скорости
    /// </summary>
    [DataField]
    public float SpeedModifierBonus = 2f;
}

