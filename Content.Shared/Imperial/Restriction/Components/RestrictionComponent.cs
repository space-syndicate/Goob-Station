using Robust.Shared.GameStates;
using Content.Shared.Whitelist;

namespace Content.Shared.Imperial.Restriction.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RestrictionSystem))]
public sealed partial class RestrictionComponent : Component
{
    [DataField("restrictions"), AutoNetworkedField]
    public EntityWhitelist? RestrictionsIDs;

    [DataField("message"), AutoNetworkedField]
    public LocId? Message;
}
