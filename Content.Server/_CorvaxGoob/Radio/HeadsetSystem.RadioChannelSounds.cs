// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Radio;
using Content.Shared._CorvaxGoob.Radio;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Robust.Shared.Audio;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Server.Radio.EntitySystems;

public sealed partial class HeadsetSystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    private static readonly SoundSpecifier DefaultRadioReceiveSound =
        new SoundPathSpecifier("/Audio/_CorvaxGoob/Radio/basic.ogg");

    /// <summary>
    /// Registers per-headset radio channel and receive-sound UI handlers.
    /// </summary>
    private void InitializeRadioChannelSounds()
    {
        SubscribeLocalEvent<HeadsetComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<HeadsetComponent, ToggleHeadsetChannelMessage>(OnToggleHeadsetChannel);
        SubscribeLocalEvent<HeadsetComponent, ToggleHeadsetChannelSoundMessage>(OnToggleHeadsetChannelSound);
        SubscribeLocalEvent<HeadsetComponent, SetAllHeadsetChannelSoundsMessage>(OnSetAllHeadsetChannelSounds);
    }

    private static bool IsChannelEnabled(HeadsetComponent component, ProtoId<RadioChannelPrototype> channel)
    {
        return !component.DisabledChannels.Contains(channel);
    }

    private static void ApplyHeadsetChannelSettings(
        HeadsetComponent component,
        HashSet<ProtoId<RadioChannelPrototype>> activeChannels)
    {
        activeChannels.ExceptWith(component.DisabledChannels);
    }

    private void TryPlayRadioReceiveSound(
        HeadsetComponent component,
        RadioReceiveEvent args,
        ProtoId<RadioChannelPrototype> channel,
        INetChannel recipient)
    {
        if (component.MutedReceiveSoundChannels.Contains(channel))
            return;

        RaiseNetworkEvent(new PlayRadioReceiveSoundEvent
        {
            Sound = args.Channel.ReceiveSound ?? DefaultRadioReceiveSound,
        }, recipient);
    }

    private void OnUiOpened(EntityUid uid, HeadsetComponent component, BoundUIOpenedEvent args)
    {
        UpdateUserInterface(uid, component);
    }

    private void OnToggleHeadsetChannel(EntityUid uid, HeadsetComponent component, ref ToggleHeadsetChannelMessage args)
    {
        if (!TryComp(uid, out EncryptionKeyHolderComponent? keyHolder) ||
            !keyHolder.Channels.Contains(args.Channel))
            return;

        var changed = args.Enabled
            ? component.DisabledChannels.Remove(args.Channel)
            : component.DisabledChannels.Add(args.Channel);

        if (!changed)
            return;

        Dirty(uid, component);
        UpdateRadioChannels(uid, component, keyHolder);
        UpdateUserInterface(uid, component, keyHolder);
    }

    private void OnToggleHeadsetChannelSound(EntityUid uid, HeadsetComponent component, ref ToggleHeadsetChannelSoundMessage args)
    {
        if (!TryComp(uid, out EncryptionKeyHolderComponent? keyHolder) ||
            !keyHolder.Channels.Contains(args.Channel))
            return;

        var changed = args.Enabled
            ? component.MutedReceiveSoundChannels.Remove(args.Channel)
            : component.MutedReceiveSoundChannels.Add(args.Channel);

        if (!changed)
            return;

        Dirty(uid, component);
        UpdateUserInterface(uid, component, keyHolder);
    }

    private void OnSetAllHeadsetChannelSounds(EntityUid uid, HeadsetComponent component, ref SetAllHeadsetChannelSoundsMessage args)
    {
        if (!TryComp(uid, out EncryptionKeyHolderComponent? keyHolder))
            return;

        var changed = args.Enabled
            ? component.MutedReceiveSoundChannels.RemoveWhere(channel => keyHolder.Channels.Contains(channel)) > 0
            : AddChannels(component.MutedReceiveSoundChannels, keyHolder.Channels);

        if (!changed)
            return;

        Dirty(uid, component);
        UpdateUserInterface(uid, component, keyHolder);
    }

    private void UpdateUserInterface(EntityUid uid, HeadsetComponent? component = null, EncryptionKeyHolderComponent? keyHolder = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!TryComp(uid, out keyHolder))
        {
            _ui.SetUiState(uid, HeadsetUiKey.Key, new HeadsetBoundUserInterfaceState(new List<HeadsetChannelState>(0)));
            return;
        }

        SanitizeChannelSettings(uid, component, keyHolder);

        var channels = new List<HeadsetChannelState>(keyHolder.Channels.Count);
        foreach (var channel in keyHolder.Channels)
        {
            channels.Add(new HeadsetChannelState(
                channel,
                IsChannelEnabled(component, channel),
                !component.MutedReceiveSoundChannels.Contains(channel)));
        }

        _ui.SetUiState(uid, HeadsetUiKey.Key, new HeadsetBoundUserInterfaceState(channels));
    }

    private void SanitizeChannelSettings(EntityUid uid, HeadsetComponent component, EncryptionKeyHolderComponent keyHolder)
    {
        var changed = false;
        changed |= component.DisabledChannels.RemoveWhere(channel => !keyHolder.Channels.Contains(channel)) > 0;
        changed |= component.MutedReceiveSoundChannels.RemoveWhere(channel => !keyHolder.Channels.Contains(channel)) > 0;

        if (changed)
            Dirty(uid, component);
    }

    private static bool AddChannels(
        HashSet<ProtoId<RadioChannelPrototype>> target,
        IReadOnlyCollection<ProtoId<RadioChannelPrototype>> channels)
    {
        var changed = false;
        foreach (var channel in channels)
        {
            changed |= target.Add(channel);
        }

        return changed;
    }
}
