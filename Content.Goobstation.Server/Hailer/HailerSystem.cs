// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Hailer;
using Content.Server.Chat.Systems;
using Content.Shared._CorvaxGoob.TTS;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Chat;
using Content.Shared.Emag.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using System.Linq;

namespace Content.Goobstation.Server.Hailer;

public sealed class HailerSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    // CorvaxGoob-HailerRework-Start
    //[Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly EmagSystem _emag = default!;
    // CorvaxGoob-HailerRework-End
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActionsComponent, HailerActionEvent>(OnHail);
        SubscribeLocalEvent<HailerComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<HailerComponent, GotUnequippedEvent>(OnGotUnequipped);

        // CorvaxGoob-HailerRework-Start
        SubscribeLocalEvent<HailerComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerb);
        SubscribeLocalEvent<HailerComponent, GotEmaggedEvent>(OnHailerEmagged);
        SubscribeLocalEvent<HailerComponent, InventoryRelayedEvent<TransformSpeakerVoiceEvent>>(OnSpeakerVoiceTransform);
        // CorvaxGoob-HailerRework-End
    }
    private void OnGotEquipped(EntityUid uid, HailerComponent component, GotEquippedEvent args)
    {
        if (args.SlotFlags == SlotFlags.MASK)
        {
            _actionsSystem.AddAction(args.Equipee, ref component.HailActionEntity, component.HailerAction, args.Equipee);
            component.SelectedMode = component.AvailableModes.Keys.First(); // CorvaxGoob-HailerRework
        }
    }
    private void OnGotUnequipped(EntityUid uid, HailerComponent component, GotUnequippedEvent args)
    {
        if (args.SlotFlags == SlotFlags.MASK)
        {
            _actionsSystem.RemoveAction(args.Equipee, component.HailActionEntity);
        }
    }

    // CorvaxGoob-HailerRework
/*    string[] _sounds = [
        "/Audio/_Goobstation/Hailer/asshole.ogg",
        "/Audio/_Goobstation/Hailer/bash.ogg",
        "/Audio/_Goobstation/Hailer/bobby.ogg",
        "/Audio/_Goobstation/Hailer/compliance.ogg",
        "/Audio/_Goobstation/Hailer/dontmove.ogg",
        "/Audio/_Goobstation/Hailer/dredd.ogg",
        "/Audio/_Goobstation/Hailer/floor.ogg",
        "/Audio/_Goobstation/Hailer/freeze.ogg",
        "/Audio/_Goobstation/Hailer/halt.ogg",
    ];*/
    Dictionary<EntityUid, TimeSpan> _delays = new Dictionary<EntityUid, TimeSpan>();
    TimeSpan _fixed_delay = TimeSpan.FromSeconds(2);
    private void OnHail(EntityUid uid, ActionsComponent component, ref HailerActionEvent args)
    {
        if (args.Handled)
            return;
        // No hail spam check.
        if (_delays.ContainsKey(uid))
        {
            if (_timing.CurTime < _delays[uid])
            {
                return;
            }
        }

        // CorvaxGoob-HailerRework-Start
        _delays[uid] = _timing.CurTime.Add(_fixed_delay);

        var inv = EntityManager.System<InventorySystem>();

        if (!inv.TryGetSlotEntity(uid, "mask", out var maskUid) || !TryComp<HailerComponent>(maskUid, out var hailer))
            return;

        if (hailer.SelectedMode is null)
            return;

        if (!hailer.AvailableModes.TryGetValue(hailer.SelectedMode, out var selectedModeDataset))
            return;

        if (!_proto.Resolve(selectedModeDataset, out var messages))
            return;

        _chat.TrySendInGameICMessage(uid, Loc.GetString(_random.Pick(messages.Values)), InGameICChatType.Speak, ChatTransmitRange.GhostRangeLimit, checkRadioPrefix: false);
        /*
                int rInt = (int) _random.NextDouble(0, _sounds.Length);
                _audio.PlayPvs(_sounds[rInt], uid);
                _chat.TrySendInGameICMessage(uid, Loc.GetString("hail-" + rInt), InGameICChatType.Speak, ChatTransmitRange.GhostRangeLimit, nameOverride: Name(uid) + "(SecMask)", checkRadioPrefix: false);*/
        // CorvaxGoob-HailerRework-End
    }

    // CorvaxGoob-HailerRework-Stat
    private void OnSpeakerVoiceTransform(Entity<HailerComponent> entity, ref InventoryRelayedEvent<TransformSpeakerVoiceEvent> ev)
    {
        if (!HasComp<EmaggedComponent>(entity))
            return;

        if (entity.Comp.EmaggedTTS is null)
            return;

        ev.Args.VoiceId = entity.Comp.EmaggedTTS;
    }

    private void OnHailerEmagged(Entity<HailerComponent> ent, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;

        if (_emag.CheckFlag(ent, EmagType.Interaction))
            return;

        if (ent.Comp.EmagedModes is null)
            return;

        foreach (var emagMode in ent.Comp.EmagedModes.Keys)
        {
            if (ent.Comp.AvailableModes.ContainsKey(emagMode))
                continue;

            ent.Comp.AvailableModes.Add(emagMode, ent.Comp.EmagedModes[emagMode]);
        }

        args.Handled = true;
    }

    private void OnGetVerb(Entity<HailerComponent> entity, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess
            || !args.CanInteract
            || !args.CanComplexInteract)
            return;

        var priority = 0;
        foreach (var entry in entity.Comp.AvailableModes)
        {
            AlternativeVerb selection = new()
            {
                Text = Loc.GetString($"hail-mode-{entry.Key}-name"),
                Category = VerbCategory.SelectType,
                Priority = priority,
                Act = () => entity.Comp.SelectedMode = entry.Key
            };

            priority--;
            args.Verbs.Add(selection);
        }
    }
    // CorvaxGoob-HailerRework-End
}
