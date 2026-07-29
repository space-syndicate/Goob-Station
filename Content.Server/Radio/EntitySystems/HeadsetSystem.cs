// SPDX-License-Identifier: MIT

using Content.Server.Chat.Systems;
using Content.Goobstation.Shared.Radio;
using Content.Shared.Radio.Components;
using Content.Server._EinsteinEngines.Language;
using Content.Shared.Chat;
using Content.Shared.Inventory.Events;
using Content.Shared.Radio;
using Content.Shared.Radio.EntitySystems;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.Radio.EntitySystems;

public sealed class HeadsetSystem : SharedHeadsetSystem
{
    [Dependency] private readonly INetManager _netMan = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly LanguageSystem _language = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!; // Goobstation
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    private static readonly SoundSpecifier DefaultRadioReceiveSound =
        new SoundPathSpecifier("/Audio/_Goobstation/Radio/basic.ogg");

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HeadsetComponent, RadioReceiveEvent>(OnHeadsetReceive);
        SubscribeLocalEvent<HeadsetComponent, EncryptionChannelsChangedEvent>(OnKeysChanged);

        SubscribeLocalEvent<WearingHeadsetComponent, EntitySpokeEvent>(OnSpeak);
        SubscribeLocalEvent<HeadsetComponent, RadioReceiveAttemptEvent>(OnHeadsetReceiveAttempt); // Goobstation - Whitelisted radio channel
        SubscribeLocalEvent<HeadsetComponent, BoundUIOpenedEvent>(OnUiOpened); // Goobstation - Headset channel controls
        SubscribeLocalEvent<HeadsetComponent, ToggleHeadsetChannelMessage>(OnToggleHeadsetChannel); // Goobstation - Headset channel controls
        SubscribeLocalEvent<HeadsetComponent, ToggleHeadsetChannelSoundMessage>(OnToggleHeadsetChannelSound); // Goobstation - Headset channel controls
        SubscribeLocalEvent<HeadsetComponent, SetAllHeadsetChannelSoundsMessage>(OnSetAllHeadsetChannelSounds); // Goobstation - Headset channel controls
    }

    private void OnKeysChanged(EntityUid uid, HeadsetComponent component, EncryptionChannelsChangedEvent args)
    {
        SanitizeChannelSettings(uid, component, args.Component);
        UpdateRadioChannels(uid, component, args.Component);
        UpdateUserInterface(uid, component, args.Component);
    }

    private void UpdateRadioChannels(EntityUid uid, HeadsetComponent headset, EncryptionKeyHolderComponent? keyHolder = null)
    {
        // make sure to not add ActiveRadioComponent when headset is being deleted
        if (!headset.Enabled || MetaData(uid).EntityLifeStage >= EntityLifeStage.Terminating)
            return;

        if (!Resolve(uid, ref keyHolder))
            return;

        var activeChannels = new HashSet<ProtoId<RadioChannelPrototype>>(keyHolder.Channels);
        activeChannels.ExceptWith(headset.DisabledChannels);

        if (activeChannels.Count == 0)
            RemComp<ActiveRadioComponent>(uid);
        else
            EnsureComp<ActiveRadioComponent>(uid).Channels = activeChannels;
    }

    private void OnSpeak(EntityUid uid, WearingHeadsetComponent component, EntitySpokeEvent args)
    {
        if (args.Channel == null)
            return;

        var channelId = new ProtoId<RadioChannelPrototype>(args.Channel.ID);

        if (TryComp(component.Headset, out HeadsetComponent? headset)
            && TryComp(component.Headset, out EncryptionKeyHolderComponent? keys)
            && keys.Channels.Contains(channelId)
            && !headset.DisabledChannels.Contains(channelId)
            && _whitelist.IsWhitelistPassOrNull(args.Channel.SendWhitelist, uid)) // Goobstation - Whitelisted channels
        {
            _radio.SendRadioMessage(uid, args.Message, args.Channel, component.Headset);
            args.Channel = null; // prevent duplicate messages from other listeners.
        }
    }

    protected override void OnGotEquipped(EntityUid uid, HeadsetComponent component, GotEquippedEvent args)
    {
        base.OnGotEquipped(uid, component, args);
        if (component.IsEquipped && component.Enabled)
        {
            EnsureComp<WearingHeadsetComponent>(args.Equipee).Headset = uid;
            UpdateRadioChannels(uid, component);
        }
    }

    protected override void OnGotUnequipped(EntityUid uid, HeadsetComponent component, GotUnequippedEvent args)
    {
        base.OnGotUnequipped(uid, component, args);
        RemComp<ActiveRadioComponent>(uid);
        RemComp<WearingHeadsetComponent>(args.Equipee);
    }

    public void SetEnabled(EntityUid uid, bool value, HeadsetComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.Enabled == value)
            return;

        component.Enabled = value;
        Dirty(uid, component);

        if (!value)
        {
            RemCompDeferred<ActiveRadioComponent>(uid);

            if (component.IsEquipped)
                RemCompDeferred<WearingHeadsetComponent>(Transform(uid).ParentUid);
        }
        else if (component.IsEquipped)
        {
            EnsureComp<WearingHeadsetComponent>(Transform(uid).ParentUid).Headset = uid;
            UpdateRadioChannels(uid, component);
        }
    }

    private void OnHeadsetReceive(EntityUid uid, HeadsetComponent component, ref RadioReceiveEvent args)
    {
        var channelId = new ProtoId<RadioChannelPrototype>(args.Channel.ID);

        if (component.DisabledChannels.Contains(channelId))
            return;

        // TODO: change this when a code refactor is done
        // this is currently done this way because receiving radio messages on an entity otherwise requires that entity
        // to have an ActiveRadioComponent

        // Einstein Engines - Language begin
        var parent = Transform(uid).ParentUid;

        if (parent.IsValid())
        {
            var relayEvent = new HeadsetRadioReceiveRelayEvent(args);
            RaiseLocalEvent(parent, ref relayEvent);
        }

        if (TryComp(parent, out ActorComponent? actor))
        {
            var canUnderstand = _language.CanUnderstand(parent, args.Language.ID);
            var msg = new MsgChatMessage
            {
                Message = canUnderstand ? args.OriginalChatMsg : args.LanguageObfuscatedChatMsg
            };
            _netMan.ServerSendMessage(msg, actor.PlayerSession.Channel);

            if (!component.MutedReceiveSoundChannels.Contains(channelId))
            {
                RaiseNetworkEvent(new PlayRadioBarkEvent
                {
                    Sound = args.Channel.ReceiveSound ?? DefaultRadioReceiveSound,
                }, actor.PlayerSession.Channel);
            }
        }
        // Einstein Engines - Language end
    }

    // Goobstation - Whitelisted radio channel
    private void OnHeadsetReceiveAttempt(EntityUid uid, HeadsetComponent component, ref RadioReceiveAttemptEvent args)
    {
        var channelId = new ProtoId<RadioChannelPrototype>(args.Channel.ID);
        args.Cancelled |= component.DisabledChannels.Contains(channelId);
        args.Cancelled |= _whitelist.IsWhitelistFail(args.Channel.ReceiveWhitelist, uid);
    }

    // Goobstation - Headset channel controls
    private void OnUiOpened(EntityUid uid, HeadsetComponent component, BoundUIOpenedEvent args)
    {
        UpdateUserInterface(uid, component);
    }

    private void OnToggleHeadsetChannel(EntityUid uid, HeadsetComponent component, ref ToggleHeadsetChannelMessage args)
    {
        if (!TryComp(uid, out EncryptionKeyHolderComponent? keyHolder) ||
            !keyHolder.Channels.Contains(args.Channel))
            return;

        if (args.Enabled)
            component.DisabledChannels.Remove(args.Channel);
        else
            component.DisabledChannels.Add(args.Channel);

        Dirty(uid, component);
        UpdateRadioChannels(uid, component, keyHolder);
        UpdateUserInterface(uid, component, keyHolder);
    }

    private void OnToggleHeadsetChannelSound(EntityUid uid, HeadsetComponent component, ref ToggleHeadsetChannelSoundMessage args)
    {
        if (!TryComp(uid, out EncryptionKeyHolderComponent? keyHolder) ||
            !keyHolder.Channels.Contains(args.Channel))
            return;

        if (args.Enabled)
            component.MutedReceiveSoundChannels.Remove(args.Channel);
        else
            component.MutedReceiveSoundChannels.Add(args.Channel);

        Dirty(uid, component);
        UpdateUserInterface(uid, component, keyHolder);
    }

    private void OnSetAllHeadsetChannelSounds(EntityUid uid, HeadsetComponent component, ref SetAllHeadsetChannelSoundsMessage args)
    {
        if (!TryComp(uid, out EncryptionKeyHolderComponent? keyHolder))
            return;

        if (args.Enabled)
            component.MutedReceiveSoundChannels.ExceptWith(keyHolder.Channels);
        else
            component.MutedReceiveSoundChannels.UnionWith(keyHolder.Channels);

        Dirty(uid, component);
        UpdateUserInterface(uid, component, keyHolder);
    }

    private void UpdateUserInterface(EntityUid uid, HeadsetComponent? component = null, EncryptionKeyHolderComponent? keyHolder = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var channels = new List<HeadsetChannelState>();

        if (TryComp(uid, out keyHolder))
        {
            SanitizeChannelSettings(uid, component, keyHolder);

            foreach (var channel in keyHolder.Channels)
            {
                channels.Add(new HeadsetChannelState(
                    channel,
                    !component.DisabledChannels.Contains(channel),
                    !component.MutedReceiveSoundChannels.Contains(channel)));
            }
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
}
