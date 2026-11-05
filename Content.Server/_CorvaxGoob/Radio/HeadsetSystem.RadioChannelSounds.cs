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
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

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

    private void OnUiOpened(Entity<HeadsetComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUserInterface(ent);
    }

    private void OnToggleHeadsetChannel(Entity<HeadsetComponent> ent, ref ToggleHeadsetChannelMessage args)
    {
        if (!TryComp(ent.Owner, out EncryptionKeyHolderComponent? keyHolder) ||
            !keyHolder.Channels.Contains(args.Channel))
            return;

        var changed = args.Enabled
            ? ent.Comp.DisabledChannels.Remove(args.Channel)
            : ent.Comp.DisabledChannels.Add(args.Channel);

        if (!changed)
            return;

        Dirty(ent);
        UpdateRadioChannels(ent.Owner, ent.Comp, keyHolder);
        UpdateUserInterface(ent, keyHolder);
    }

    private void OnToggleHeadsetChannelSound(Entity<HeadsetComponent> ent, ref ToggleHeadsetChannelSoundMessage args)
    {
        if (!TryComp(ent.Owner, out EncryptionKeyHolderComponent? keyHolder) ||
            !keyHolder.Channels.Contains(args.Channel))
            return;

        var changed = args.Enabled
            ? ent.Comp.MutedReceiveSoundChannels.Remove(args.Channel)
            : ent.Comp.MutedReceiveSoundChannels.Add(args.Channel);

        if (!changed)
            return;

        Dirty(ent);
        UpdateUserInterface(ent, keyHolder);
    }

    private void OnSetAllHeadsetChannelSounds(Entity<HeadsetComponent> ent, ref SetAllHeadsetChannelSoundsMessage args)
    {
        if (!TryComp(ent.Owner, out EncryptionKeyHolderComponent? keyHolder))
            return;

        var changed = args.Enabled
            ? ent.Comp.MutedReceiveSoundChannels.RemoveWhere(channel => keyHolder.Channels.Contains(channel)) > 0
            : AddChannels(ent.Comp.MutedReceiveSoundChannels, keyHolder.Channels);

        if (!changed)
            return;

        Dirty(ent);
        UpdateUserInterface(ent, keyHolder);
    }

    private void UpdateUserInterface(Entity<HeadsetComponent> ent, EncryptionKeyHolderComponent? keyHolder = null)
    {
        if (!Resolve(ent.Owner, ref keyHolder))
            return;

        SanitizeChannelSettings(ent, keyHolder);

        var channels = new List<HeadsetChannelState>(keyHolder.Channels.Count);
        foreach (var channel in keyHolder.Channels)
        {
            channels.Add(new HeadsetChannelState(
                channel,
                IsChannelEnabled(ent.Comp, channel),
                !ent.Comp.MutedReceiveSoundChannels.Contains(channel)));
        }

        _ui.SetUiState(ent.Owner, HeadsetUiKey.Key, new HeadsetBoundUserInterfaceState(channels));
    }

    private void SanitizeChannelSettings(Entity<HeadsetComponent> ent, EncryptionKeyHolderComponent keyHolder)
    {
        var changed = false;
        changed |= ent.Comp.DisabledChannels.RemoveWhere(channel => !keyHolder.Channels.Contains(channel)) > 0;
        changed |= ent.Comp.MutedReceiveSoundChannels.RemoveWhere(channel => !keyHolder.Channels.Contains(channel)) > 0;

        if (changed)
            Dirty(ent);
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
