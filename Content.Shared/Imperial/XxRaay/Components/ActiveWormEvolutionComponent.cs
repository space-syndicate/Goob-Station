using Content.Shared.Imperial.XxRaay.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Imperial.XxRaay.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedWormEvolutionSystem))]
public sealed partial class ActiveWormEvolutionComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid Cocoon;

    [DataField, AutoNetworkedField]
    public bool PlayerControlled;
}
