using Robust.Shared.GameStates;

namespace Content.Shared.Imperial.MindPacified.Components;

/// <summary>
/// Компонент, использующийся для пометки того разума сущности,
/// который должен получить компонент MindPacified в свой разум после взаимодействия с чем-либо.
/// </summary>

[RegisterComponent, NetworkedComponent]
public sealed partial class GiveMindPacifiedComponent : Component
{

}
