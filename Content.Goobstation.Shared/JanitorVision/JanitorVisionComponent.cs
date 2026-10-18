// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.JanitorVision;

/// <summary>Equipment that highlights loose trash and puddles through walls.</summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class JanitorVisionComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    /// <summary>Detection radius in metres, within the normal client PVS.</summary>
    [DataField, AutoNetworkedField]
    public float Range = 8f;

    [DataField]
    public EntProtoId ToggleAction = "ActionToggleJanitorVision";

    [DataField, AutoNetworkedField]
    public EntityUid? ToggleActionEntity;
}

public sealed partial class ToggleJanitorVisionEvent : InstantActionEvent;

public enum JanitorVisionTarget : byte
{
    Trash,
    Puddle,
}

[ByRefEvent]
public readonly record struct JanitorVisionChangedEvent;
