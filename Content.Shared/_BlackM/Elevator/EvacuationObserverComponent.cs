using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._BlackM.Elevator;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class EvacuationObserverComponent : Component
{
    [ViewVariables]
    public EntityUid Body;

    [DataField, AutoNetworkedField]
    public bool IsObserving;
}

[Serializable, NetSerializable]
public sealed class EvacuationObserverRequestEvent(bool returnToBody) : EntityEventArgs
{
    public bool ReturnToBody = returnToBody;
}
