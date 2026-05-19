using Robust.Shared.GameStates;

namespace Content.Shared.Imperial.XxRaay.Components;

/// <summary>
/// Помечает сущность: её реплики выводятся от имени ближайшей камеры.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CameraRelayChatComponent : Component
{
    /// <summary>
    /// Включает или выключает подмену имени на имя камеры.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    /// <summary>
    /// Максимальная дистанция поиска камеры; 0 или меньше — без ограничения.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MaxRange;

}
