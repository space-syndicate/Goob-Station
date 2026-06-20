using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Content.Shared.Radio;
using Robust.Shared.Audio;

namespace Content.Shared.Imperial.EmergencyButton.Components;

/// <summary>
/// Компонент для тревожной кнопки СБ.
/// При использовании отправляет сообщение в рацию СБ с информацией о местоположении.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
[AutoGenerateComponentPause]
public sealed partial class EmergencyButtonComponent : Component
{
    /// <summary>
    /// Максимальное количество зарядов кнопки.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int MaxCharges = 1;

    /// <summary>
    /// Текущее количество зарядов кнопки.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int CurrentCharges = 1;

    /// <summary>
    /// Радиоканал, в который отправляется тревожное сообщение.
    /// </summary>
    [DataField]
    public ProtoId<RadioChannelPrototype> RadioChannel = "Security";

    /// <summary>
    /// Время сброса состояния кнопки
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan? NextUnprime;

    /// <summary>
    /// Задержка сброса состояния кнопки
    /// </summary>
    [DataField]
    public TimeSpan PrimeTime = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Звук при активации кнопки
    /// </summary>
    [DataField]
    public SoundSpecifier UseSound = new SoundPathSpecifier("/Audio/Voice/Talk/Silicon/syndieborg_exclaim.ogg");
}
