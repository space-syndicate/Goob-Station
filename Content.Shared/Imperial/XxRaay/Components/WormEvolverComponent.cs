using Content.Shared.Imperial.XxRaay.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Imperial.XxRaay.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedWormEvolutionSystem))]
public sealed partial class WormEvolverComponent : Component
{
    [DataField]
    public EntProtoId EvolutionAction = "ActionWormEvolution";

    [DataField, AutoNetworkedField]
    public EntityUid? EvolutionActionEntity;

    [DataField]
    public int BloodCost = 100;

    [DataField]
    public float WeaveDelay = 3f;

    [DataField]
    public float EvolutionDuration = 90f;

    [DataField]
    public EntProtoId CocoonProto;

    [DataField]
    public EntProtoId ResultProto;

    [DataField]
    public EntProtoId SourceProto;
}
