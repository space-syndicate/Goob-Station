using Content.Shared.Imperial.XxRaay.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Imperial.XxRaay.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedWormCorpsePossessionSystem))]
public sealed partial class WormCorpseOccupiedComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid Worm;

    [DataField, AutoNetworkedField]
    public EntityUid? ExitActionEntity;

    [DataField, AutoNetworkedField]
    public bool AddedStatusIcon;
}
