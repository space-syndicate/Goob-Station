using Content.Shared.Imperial.XxRaay.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Imperial.XxRaay.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedWormReproductionSystem))]
public sealed partial class WormReproducerComponent : Component
{
    [DataField]
    public EntProtoId ReproductionAction = "ActionWormReproduction";

    [DataField, AutoNetworkedField]
    public EntityUid? ReproductionActionEntity;

    [DataField]
    public int BloodCost = 300;

    [DataField]
    public float WeaveDelay = 3f;

    [DataField]
    public float ReproductionDuration = 120f;

    [DataField]
    public EntProtoId CocoonProto;

    [DataField]
    public EntProtoId OffspringProto;

    [DataField]
    public int OffspringCount = 3;

    [DataField]
    public EntProtoId ParentResultProto;

    [DataField]
    public EntProtoId SourceProto;
}
