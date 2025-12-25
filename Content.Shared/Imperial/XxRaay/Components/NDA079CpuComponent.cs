using Robust.Shared.GameStates;

namespace Content.Shared.Imperial.XxRaay.Components;

/// <summary>
/// Компонент, хранящий уровень и текущие CPU
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class NDA079CpuComponent : Component
{
    /// <summary>
    /// Текущий уровень
    /// </summary>
    [DataField, AutoNetworkedField]
    public int CurrentLevel = 1;

    /// <summary>
    /// Текущее количество накопленных CPU поинтов до следующего уровня.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int CurrentCpu = 0;

    /// <summary>
    /// Требуемое количество CPU поинтов до следующего уровня.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int[] LevelRequirements = { 0, 20, 50, 90 };

    /// <summary>
    /// Максимальный уровень.
    /// </summary>
    public int LvlMax = 4;
}


