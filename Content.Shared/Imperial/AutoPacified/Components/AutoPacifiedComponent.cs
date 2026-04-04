using Robust.Shared.GameStates;

namespace Content.Shared.Imperial.AutoPacified.Components;

/// <summary>
/// Компонент, использующийся для пометки того,
/// кто должен получить пацифизм даже после перемещения разума.
/// </summary>

[RegisterComponent, NetworkedComponent]
public sealed partial class AutoPacifiedComponent : Component
{

}
