using Robust.Shared.GameStates;

namespace Content.Shared._CorvaxGoob.Ghost;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GhostGoLobbyComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan AvailableAt;
}
