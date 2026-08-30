// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._CorvaxGoob.Radio;
using Content.Shared.Radio;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client._CorvaxGoob.Radio.UI;

/// <summary>
/// Client bridge for the per-headset radio channel settings menu.
/// </summary>
public sealed partial class HeadsetBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private HeadsetMenu? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<HeadsetMenu>();
        _window.OnChannelToggled += OnChannelToggled;
        _window.OnSoundToggled += OnSoundToggled;
        _window.OnAllSoundsToggled += OnAllSoundsToggled;

        if (State is HeadsetBoundUserInterfaceState state)
            _window.UpdateState(state);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not HeadsetBoundUserInterfaceState headsetState)
            return;

        _window?.UpdateState(headsetState);
    }

    private void OnChannelToggled(ProtoId<RadioChannelPrototype> channel, bool enabled)
    {
        SendMessage(new ToggleHeadsetChannelMessage(channel, enabled));
    }

    private void OnSoundToggled(ProtoId<RadioChannelPrototype> channel, bool enabled)
    {
        SendMessage(new ToggleHeadsetChannelSoundMessage(channel, enabled));
    }

    private void OnAllSoundsToggled(bool enabled)
    {
        SendMessage(new SetAllHeadsetChannelSoundsMessage(enabled));
    }
}
