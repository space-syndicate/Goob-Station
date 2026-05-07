using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Content.Shared.Actions;

namespace Content.Shared.Imperial.XenoGenetics.Genes.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GrantActionsGeneComponent : Component
{
    /// <summary>
    /// Трешхолд для применения эффекта
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Threshold = 0.5f;

    /// <summary>
    /// Базовый модифаер
    /// </summary>
    [DataField, AutoNetworkedField]
    public float BasicModifier = 1.2f;

    /// <summary>
    /// Будет ли рандомится КД в зависимости от качества гена. кд * (BasicModifier - качество)
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool RandomCooldown = true;

    /// <summary>
    /// Акшены
    /// </summary>
    [DataField(required: true), AutoNetworkedField, AlwaysPushInheritance]
    public List<EntProtoId> ActionsList = new();

    /// <summary>
    /// Энтити акшенов
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<EntityUid> EntityList = new();
}