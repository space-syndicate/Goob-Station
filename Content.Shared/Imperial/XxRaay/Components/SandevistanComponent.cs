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
    [AutoPausedField]
    public TimeSpan EffectEndTime;

    /// <summary>
    /// Время начала перезарядки.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    [AutoPausedField]
    public TimeSpan CooldownStartTime;

    /// <summary>
    /// Время окончания перезарядки.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    [AutoPausedField]
    public TimeSpan CooldownEndTime;

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
    [DataField, AutoNetworkedField]
    public TimeSpan EffectDuration = TimeSpan.FromSeconds(8);

    /// <summary>
    /// Кулдаун
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan CooldownDuration = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Бонус к скорости
    /// </summary>
    [DataField, AutoNetworkedField]
    public float SpeedModifierBonus = 2f;
}

