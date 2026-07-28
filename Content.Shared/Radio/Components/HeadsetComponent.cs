// SPDX-License-Identifier: MIT

using Content.Shared.Inventory;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Radio.Components;

/// <summary>
/// This component relays radio messages to the parent entity's chat when equipped.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HeadsetComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    [DataField, AutoNetworkedField]
    public bool IsEquipped = false;

    [DataField, AutoNetworkedField]
    public SlotFlags RequiredSlot = SlotFlags.EARS;

    // Goobstation - Headset channel controls
    /// <summary>
    /// Radio channels disabled on this headset without removing encryption keys.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<RadioChannelPrototype>> DisabledChannels = new();

    /// <summary>
    /// Radio channels that should not play receive sounds on this headset.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<RadioChannelPrototype>> MutedReceiveSoundChannels = new();
}
