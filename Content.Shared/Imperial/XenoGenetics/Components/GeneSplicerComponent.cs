using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.XenoGenetics.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GeneSplicerComponent : Component
{
    /// <summary>
    /// Айдишник слота спайщика генов
    /// </summary>
    [DataField, AutoNetworkedField]
    public string GeneContainerID = "gene_slot";

    /// <summary>
    /// Айдишник слота для генов у энтити
    /// </summary>
    [DataField, AutoNetworkedField]
    public string EntityGeneContainerID = "gene_entity_slot";

    /// <summary>
    /// Вводить/доставать
    /// </summary>
    [DataField, AutoNetworkedField]
    public GeneSplicerMode InsertMode = GeneSplicerMode.Insert;

}
[Serializable, NetSerializable]
public enum GeneSplicerMode : byte
{
    /// <summary>
    /// При нажатии по энтити начнет дуафтер чтобы вставить ему ген
    /// </summary>
    Insert = 0,
    /// <summary>
    /// При нажатии на энтити заберет ген из него
    /// </summary>
    Withdraw = 1,

}

