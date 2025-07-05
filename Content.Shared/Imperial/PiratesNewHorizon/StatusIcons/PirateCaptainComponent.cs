using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Imperial.PiratesNewHorizon.StatusIcons;

[RegisterComponent, NetworkedComponent]
public sealed partial class PirateCaptainComponent : Component
{
    [DataField]
    public ProtoId<FactionIconPrototype> StatusIcon = "PirateCaptainFaction";
}