using Content.Shared.Imperial.XxRaay.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Imperial.XxRaay.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedWormBloodDrinkSystem))]
public sealed partial class WormBloodDrinkingComponent : Component
{
    [DataField, AutoNetworkedField]
    public NetEntity Target;
}
