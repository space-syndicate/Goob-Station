using Robust.Shared.GameStates;

namespace Content.Shared._CorvaxGoob.Hostage;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HostageComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid Taker;

    [DataField, AutoNetworkedField]
    public EntityUid Gun;
}
