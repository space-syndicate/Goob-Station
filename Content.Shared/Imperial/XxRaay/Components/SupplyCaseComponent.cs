using Robust.Shared.GameStates;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;

namespace Content.Shared.Imperial.XxRaay.Components;

/// <summary>
/// Component for supply case that can send items via supplypod.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SupplyCaseComponent : Component
{
    [DataField]
    public EntProtoId PodPrototype = "supplypod_spawn";

    [DataField]
    public LocId SendVerbLoc = new("supplycase-verb-send");

    [DataField]
    public LocId EmptyCaseLoc = new("supplycase-empty");

    [DataField]
    public LocId SendDescLoc = new("supplycase-send-desc");

    [DataField]
    public LocId PopupSentLoc = new("supplycase-popup-sent");
}

