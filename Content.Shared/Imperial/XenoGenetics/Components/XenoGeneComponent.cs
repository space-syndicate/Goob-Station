using Robust.Shared.GameStates;

namespace Content.Shared.Imperial.XenoGenetics.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoGeneComponent : Component
{
    /// <summary>
    /// Определяет модификатор применяемый к статам. Смотрите в коде компонентов генов подробнее что к чему.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float geneMultiplier;
    
    /// <summary>
    /// Определяет будет ли geneMultiplier рандомиться.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool randomizeGeneQuality = true;

}

