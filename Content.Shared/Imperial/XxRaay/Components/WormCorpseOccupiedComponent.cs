using Content.Shared.Imperial.XxRaay.Systems;
using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

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

    [DataField]
    public ProtoId<FactionIconPrototype> FactionIcon = "WormFaction";
}
