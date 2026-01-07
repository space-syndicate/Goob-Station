using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.XenoGenetics.Genes.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RespiratoryAdaptationGeneComponent : Component
{
    /// <summary>
    /// порог качества после которого индивиду больше не нужно дышать
    /// </summary>
    [DataField, AutoNetworkedField]
    public float saturationThreshold = 75f;

    [DataField, AutoNetworkedField]
    public bool works = false;
    [DataField, AutoNetworkedField]
    public EntityUid Target;
}