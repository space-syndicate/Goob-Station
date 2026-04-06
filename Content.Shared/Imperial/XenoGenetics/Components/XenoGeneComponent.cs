using Robust.Shared.GameStates;
using Content.Shared.Alert;
using Robust.Shared.Prototypes;

namespace Content.Shared.Imperial.XenoGenetics.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoGeneComponent : Component
{
    /// <summary>
    /// Определяет модификатор применяемый к статам. Смотрите в коде компонентов генов подробнее что к чему.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float GeneMultiplier;
    
    /// <summary>
    /// Определяет будет ли geneMultiplier рандомиться.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool RandomizeGeneQuality = true;

    /// <summary>
    /// Айдишник алерта
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<AlertPrototype> AlertProto = "XenogeneInserted";

}

