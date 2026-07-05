using Robust.Shared.GameStates;

namespace Content.Shared.Imperial.XxRaay.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class WormDoorHideOccupiedComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid Worm;
}
