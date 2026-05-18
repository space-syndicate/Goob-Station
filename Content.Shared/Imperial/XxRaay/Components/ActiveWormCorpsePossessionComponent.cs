using Content.Shared.Imperial.XxRaay.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Imperial.XxRaay.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedWormCorpsePossessionSystem))]
public sealed partial class ActiveWormCorpsePossessionComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid Corpse;

    [DataField, AutoNetworkedField]
    public bool RelocatedHtn;

    [DataField, AutoNetworkedField]
    public bool CorpseHadHtn;

    [DataField, AutoNetworkedField]
    public bool PlayerControlled;
}
