using Content.Shared.Imperial.XxRaay.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Imperial.XxRaay.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
[Access(typeof(SharedWormCocoonObserveSystem))]
public sealed partial class WormCocoonObserverComponent : Component
{
    [DataField]
    public EntProtoId ObserveAction = "ActionWormCocoonObserve";

    [DataField, AutoNetworkedField]
    public EntityUid? ObserveActionEntity;

    [DataField, AutoNetworkedField]
    public EntityUid SourceWorm;

    [ViewVariables]
    public EntityUid? ObservedEntity;
}
