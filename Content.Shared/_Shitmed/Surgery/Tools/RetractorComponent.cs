// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Shared._Shitmed.Medical.Surgery.Tools;

[RegisterComponent, NetworkedComponent]
public sealed partial class RetractorComponent : Component, ISurgeryToolComponent
{
    public string ToolName => "itemswitch-component-state-retractor"; // CorvaxGoob-localization
    [DataField]
    public bool? Used { get; set; } = null;
    [DataField]
    public float Speed { get; set; } = 1f;
}
