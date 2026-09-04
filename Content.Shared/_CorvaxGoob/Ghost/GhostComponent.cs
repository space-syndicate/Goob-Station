// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Ghost;

// Observer HUD
public sealed partial class GhostComponent
{
    [DataField]
    public EntProtoId ToggleGhostObserverHudAction = "ActionToggleGhostObserverHud";

    [DataField, AutoNetworkedField]
    public EntityUid? ToggleGhostObserverHudActionEntity;
}

public sealed partial class ToggleGhostObserverHudActionEvent : InstantActionEvent { }
