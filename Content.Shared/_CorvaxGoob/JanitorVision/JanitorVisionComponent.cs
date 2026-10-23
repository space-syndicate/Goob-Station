// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CorvaxGoob.JanitorVision;

/// <summary>Equipment that highlights trash, footprints and non-evaporating puddles through walls.</summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class JanitorVisionComponent : Component
{
    /// <summary>Whether the wearer has enabled the scanner action.</summary>
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    /// <summary>Detection radius in metres, within the normal client PVS.</summary>
    [DataField, AutoNetworkedField]
    public float Range = 8f;

    /// <summary>Action granted only while equipped in the eyes slot.</summary>
    [DataField]
    public EntProtoId ToggleAction = "ActionToggleJanitorVision";

    /// <summary>Action instance owned by these goggles and shared between successive wearers.</summary>
    [DataField, AutoNetworkedField]
    public EntityUid? ToggleActionEntity;
}

public sealed partial class ToggleJanitorVisionEvent : InstantActionEvent;

[ByRefEvent]
public readonly record struct JanitorVisionChangedEvent;
