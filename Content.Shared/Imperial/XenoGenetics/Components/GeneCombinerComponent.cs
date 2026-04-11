using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.XenoGenetics.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GeneCombinerComponent : Component
{
    /// <summary>
    /// Айдишники слотов для генов
    /// </summary>
    [DataField, AutoNetworkedField]
    public string GeneContainerID1 = "gene_slot1";

    [DataField, AutoNetworkedField]
    public string GeneContainerID2 = "gene_slot2";

    /// <summary>
    /// Время на ввод
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan ProduceTime = TimeSpan.FromSeconds(2.5f);
}