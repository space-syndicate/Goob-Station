using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Imperial.Vampire;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class VampireJerkComponent : Component
{
    /// <summary>
    /// Насколько далеко вы прыгнете (в тайлах)
    /// </summary>
    [DataField, AutoNetworkedField]
    public float JumpDistance = 5f;

    /// <summary>
    /// Базовая скорость "броска" для метода TryThrow
    /// </summary>
    [DataField, AutoNetworkedField]
    public float JumpThrowSpeed = 10f;

    /// <summary>
    /// Звук, который воспроизводится при использовании способности прыжка
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? JumpSound;

    /// <summary>
    /// Всплывающее сообщение, отображаемое если сущность не может совершить прыжок
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId? JumpFailedPopup = "jump-ability-failure";
}
