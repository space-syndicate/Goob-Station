using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Audio;

namespace Content.Shared.Imperial.XenoGenetics.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GeneCombinerComponent : Component
{
    /// <summary>
    /// Айдишники слотов для генов
    /// </summary>
    [DataField, AutoNetworkedField]
    public int MaxGenes = 2;
    /// <summary>
    /// Айдишники слотов для выхода генов
    /// </summary>
    [DataField, AutoNetworkedField]
    public string GeneContainerIDOutput = "gene_output";
    /// <summary>
    /// Айдишники слотов для генов
    /// </summary>
    [DataField, AutoNetworkedField]
    public string GeneContainerID = "gene_slot";

    /// <summary>
    /// Время на ввод (это на будущее)
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan ProduceTime = TimeSpan.FromSeconds(2.5f);

    /// <summary>
    /// Когда закончится (это на будущее)
    /// </summary>
    [ViewVariables]
    public TimeSpan EndTime = TimeSpan.Zero;

    [DataField]
    public SoundSpecifier CompleteSound = new SoundPathSpecifier("/Audio/Machines/ding.ogg");
    [DataField]
    public SoundSpecifier DeclineSound = new SoundPathSpecifier("/Audio/Machines/buzz-sigh.ogg");
}
