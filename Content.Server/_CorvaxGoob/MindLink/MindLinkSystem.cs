using Content.Server.Actions;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Goobstation.Shared.Possession;
using Content.Shared._CorvaxGoob.MindLink;
using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Systems;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Players.RateLimiting;
using Content.Shared.Popups;
using Robust.Server.Player;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using System.Linq;

namespace Content.Server._CorvaxGoob.MindLink;

public sealed class MindLinkSystem : EntitySystem
{
    private static readonly EntProtoId ReplyActionPrototype = "ActionMindLinkReply";
    private const int MaxMessageLength = 256;

    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<MindLinkComponent, ComponentInit>(OnMindLinkInit);
        SubscribeLocalEvent<MindLinkComponent, ComponentShutdown>(OnMindLinkShutdown);
        SubscribeLocalEvent<MindLinkComponent, OpenMindLinkEvent>(OnOpenAction);
        SubscribeLocalEvent<MindLinkRecipientComponent, ReplyMindLinkEvent>(OnReplyAction);
        SubscribeLocalEvent<MindLinkRecipientComponent, ComponentShutdown>(OnRecipientShutdown);
        SubscribeLocalEvent<OrganComponent, OrganRemovedFromBodyEvent>(OnOrganRemoved);
        SubscribeLocalEvent<BrainComponent, ComponentShutdown>(OnBrainShutdown);
        SubscribeLocalEvent<PossessionImmuneComponent, ComponentInit>(OnPossessionImmuneInit);
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<PlayerDetachedEvent>(OnPlayerDetached);

        Subs.BuiEvents<MindLinkComponent>(MindLinkUiKey.Key, subs =>
        {
            subs.Event<SelectMindLinkTargetMessage>(OnTargetSelected);
            subs.Event<SelectAllMindLinkTargetsMessage>(OnAllTargetsSelected);
            subs.Event<DisconnectMindLinkTargetMessage>(OnTargetDisconnected);
            subs.Event<SendMindLinkMessage>(OnMessageSent);
        });
    }

    private void OnMindLinkInit(Entity<MindLinkComponent> ent, ref ComponentInit args)
    {
        ent.Comp.AddedUserInterface = !HasComp<UserInterfaceComponent>(ent);
        _ui.SetUi(ent.Owner, MindLinkUiKey.Key, new InterfaceData("MindLinkBoundUserInterface"));
    }

    private void OnMindLinkShutdown(Entity<MindLinkComponent> ent, ref ComponentShutdown args)
    {
        ClearConnections(ent);
        ClearIncomingConnections(ent.Owner);

        if (ent.Comp.AddedUserInterface)
            RemComp<UserInterfaceComponent>(ent);
    }

    private void OnRecipientShutdown(Entity<MindLinkRecipientComponent> ent, ref ComponentShutdown args)
    {
        ClearIncomingConnections(ent.Owner, ent.Comp, removeRecipient: false);
    }

    private void OnMobStateChanged(MobStateChangedEvent args)
    {
        if (args.OldMobState == MobState.Alive && args.NewMobState != MobState.Alive)
            ClearAllConnections(args.Target);
    }

    private void OnPlayerDetached(PlayerDetachedEvent args)
    {
        ClearAllConnections(args.Entity);
    }

    private void OnOrganRemoved(Entity<OrganComponent> ent, ref OrganRemovedFromBodyEvent args)
    {
        if (!HasComp<BrainComponent>(ent.Owner))
            return;

        if (!HasActiveBrain(args.OldBody, ent.Owner))
            ClearAllConnections(args.OldBody);
    }

    private void OnBrainShutdown(Entity<BrainComponent> ent, ref ComponentShutdown args)
    {
        if (TryComp(ent.Owner, out OrganComponent? organ)
            && organ.Body is { } body
            && !HasActiveBrain(body, ent.Owner))
            ClearAllConnections(body);

        if (!TryComp(ent.Owner, out BodyComponent? _)
            || !HasActiveBrain(ent.Owner, ent.Owner))
            ClearAllConnections(ent.Owner);
    }

    private void OnPossessionImmuneInit(Entity<PossessionImmuneComponent> ent, ref ComponentInit args)
    {
        ClearAllConnections(ent.Owner);
    }

    private void OnOpenAction(Entity<MindLinkComponent> ent, ref OpenMindLinkEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        if (HasComp<PossessionImmuneComponent>(ent.Owner))
        {
            _popup.PopupEntity(Loc.GetString("mind-link-blocked"), ent.Owner, ent.Owner, PopupType.MediumCaution);
            return;
        }

        ent.Comp.TwoWayCommunication = args.TwoWayCommunication;
        ent.Comp.MultiLink = args.MultiLink;
        ent.Comp.Range = args.Range;
        ent.Comp.PendingTargets.Clear();
        ent.Comp.PendingReplyTarget = null;
        ent.Comp.SelectingReplyTarget = false;
        OpenTargetSelection(ent);
    }

    private void OnReplyAction(Entity<MindLinkRecipientComponent> ent, ref ReplyMindLinkEvent args)
    {
        if (args.Handled)
            return;

        if (HasComp<PossessionImmuneComponent>(ent.Owner))
        {
            args.Handled = true;
            _popup.PopupEntity(Loc.GetString("mind-link-blocked"), ent.Owner, ent.Owner, PopupType.MediumCaution);
            return;
        }

        if (ent.Comp.ReplyInitiators.Count == 0
            || !TryComp(ent.Owner, out MindLinkComponent? link))
            return;

        args.Handled = true;
        var initiators = GetReplyTargets(ent.Owner);
        if (initiators.Count == 0)
            return;

        link.PendingReplyTarget = null;
        if (initiators.Count == 1)
        {
            var initiator = GetEntity(initiators[0].Entity);
            link.SelectingReplyTarget = false;
            link.PendingReplyTarget = initiator;
            OpenMessageWindow(ent.Owner, [initiator], isReply: true);
            return;
        }

        link.SelectingReplyTarget = true;
        OpenReplyTargetSelection(ent.Owner, initiators);
    }

    private void OnTargetSelected(Entity<MindLinkComponent> ent, ref SelectMindLinkTargetMessage args)
    {
        if (args.Actor != ent.Owner)
            return;

        if (HasComp<PossessionImmuneComponent>(ent.Owner))
        {
            _popup.PopupEntity(Loc.GetString("mind-link-blocked"), ent.Owner, ent.Owner, PopupType.MediumCaution);
            return;
        }

        var target = GetEntity(args.Target);
        if (ent.Comp.SelectingReplyTarget)
        {
            if (!TryComp(ent.Owner, out MindLinkRecipientComponent? recipient)
                || !recipient.ReplyInitiators.Contains(target)
                || !IsActiveConnection(target, ent.Owner))
            {
                OpenReplyTargetSelection(ent.Owner, GetReplyTargets(ent.Owner));
                return;
            }

            ent.Comp.SelectingReplyTarget = false;
            ent.Comp.PendingReplyTarget = target;
            OpenMessageWindow(ent.Owner, [target], isReply: true);
            return;
        }

        var isCurrentTarget = ent.Comp.Targets.Contains(target);
        if (!Exists(target) || !IsValidTarget(target)
            || !isCurrentTarget && !IsWithinRange(ent.Owner, target, ent.Comp.Range))
        {
            OpenTargetSelection(ent);
            return;
        }

        ent.Comp.PendingReplyTarget = null;
        ent.Comp.SelectingReplyTarget = false;
        if (!isCurrentTarget)
            SetTarget(ent, target);
        OpenMessageWindow(ent.Owner, [target], isReply: false);
    }

    private void OnAllTargetsSelected(Entity<MindLinkComponent> ent, ref SelectAllMindLinkTargetsMessage args)
    {
        if (args.Actor != ent.Owner || ent.Comp.SelectingReplyTarget || !ent.Comp.MultiLink)
            return;

        var targets = ent.Comp.Targets.Where(target => IsActiveConnection(ent.Owner, target)).ToList();
        if (targets.Count == 0)
        {
            OpenTargetSelection(ent);
            return;
        }

        OpenMessageWindow(ent.Owner, targets, isReply: false);
    }

    private void OnTargetDisconnected(Entity<MindLinkComponent> ent, ref DisconnectMindLinkTargetMessage args)
    {
        if (args.Actor != ent.Owner || ent.Comp.SelectingReplyTarget)
            return;

        var target = GetEntity(args.Target);
        if (!ent.Comp.Targets.Contains(target))
            return;

        ClearConnection(ent, target);
        OpenTargetSelection(ent);
    }

    private void OnMessageSent(Entity<MindLinkComponent> ent, ref SendMindLinkMessage args)
    {
        if (args.Actor != ent.Owner)
            return;

        var message = args.Message.Trim();
        var source = args.Actor;
        var isReply = ent.Comp.PendingReplyTarget is not null;
        List<EntityUid> targets;

        if (isReply)
        {
            targets = ent.Comp.PendingReplyTarget is { } pendingReplyTarget ? [pendingReplyTarget] : [];
            if (targets.Count != 1
                || !TryComp(ent.Owner, out MindLinkRecipientComponent? recipient)
                || !recipient.ReplyInitiators.Contains(targets[0])
                || !IsActiveConnection(targets[0], ent.Owner))
                return;
        }
        else
        {
            targets = ent.Comp.PendingTargets
                .Where(target => IsActiveConnection(source, target))
                .ToList();
            ent.Comp.PendingTargets = targets;

            if (targets.Count == 0)
            {
                OpenTargetSelection(ent);
                return;
            }
        }

        if (message.Length == 0 || message.Length > MaxMessageLength)
            return;

        if (!TrySendMessage(source, targets, message))
            return;

        // Also register here for already-established links loaded from older state.
        if (!isReply)
        {
            foreach (var target in targets)
                RegisterRecipient(ent, target);
        }
    }

    private void OpenTargetSelection(Entity<MindLinkComponent> ent)
    {
        var targets = GetTargets(ent);
        _ui.OpenUi(ent.Owner, MindLinkUiKey.Key, ent.Owner);
        _ui.SetUiState(ent.Owner, MindLinkUiKey.Key,
            new MindLinkBuiState(targets, null, false, ent.Comp.MultiLink && ent.Comp.Targets.Count > 0));
    }

    private void OpenReplyTargetSelection(EntityUid recipient, List<MindLinkTarget> targets)
    {
        _ui.OpenUi(recipient, MindLinkUiKey.Key, recipient);
        _ui.SetUiState(recipient, MindLinkUiKey.Key, new MindLinkBuiState(targets, null, true, false));
    }

    private void OpenMessageWindow(EntityUid uiOwner, List<EntityUid> recipients, bool isReply)
    {
        if (!TryComp(uiOwner, out MindLinkComponent? link))
            return;

        if (!isReply)
            link.PendingTargets = recipients;

        var recipientName = recipients.Count == 1 ? Name(recipients[0]) : Loc.GetString("mind-link-all-targets");
        _ui.OpenUi(uiOwner, MindLinkUiKey.Key, uiOwner);
        _ui.SetUiState(uiOwner, MindLinkUiKey.Key,
            new MindLinkBuiState([], recipientName, isReply, false));
    }

    private List<MindLinkTarget> GetTargets(Entity<MindLinkComponent> source)
    {
        var result = new List<MindLinkTarget>();
        if (HasComp<PossessionImmuneComponent>(source.Owner))
            return result;

        // Established connections are always shown first and do not depend on range.
        foreach (var current in source.Comp.Targets)
        {
            if (IsActiveConnection(source.Owner, current))
                result.Add(new MindLinkTarget(GetNetEntity(current), Name(current), true));
        }

        var available = new List<MindLinkTarget>();
        var query = EntityQueryEnumerator<MobStateComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            if (uid != source.Owner
                && !source.Comp.Targets.Contains(uid)
                && IsValidTarget(uid)
                && IsWithinRange(source.Owner, uid, source.Comp.Range))
                available.Add(new MindLinkTarget(GetNetEntity(uid), Name(uid), false));
        }

        available.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase));
        result.AddRange(available);
        return result;
    }

    private List<MindLinkTarget> GetReplyTargets(EntityUid recipient)
    {
        var result = new List<MindLinkTarget>();
        if (!TryComp(recipient, out MindLinkRecipientComponent? recipientComp))
            return result;

        foreach (var initiator in recipientComp.ReplyInitiators)
        {
            if (IsActiveConnection(initiator, recipient))
                result.Add(new MindLinkTarget(GetNetEntity(initiator), Name(initiator), false));
        }

        result.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase));
        return result;
    }

    private void SetTarget(Entity<MindLinkComponent> source, EntityUid target)
    {
        if (HasComp<PossessionImmuneComponent>(source.Owner)
            || HasComp<PossessionImmuneComponent>(target))
            return;

        if (!source.Comp.MultiLink)
            ClearConnections(source);

        if (!source.Comp.Targets.Add(target))
            return;

        _popup.PopupEntity(Loc.GetString("mind-link-connection-established"), target, target, PopupType.MediumCaution);
        RegisterRecipient(source, target);
    }

    private void RegisterRecipient(Entity<MindLinkComponent> source, EntityUid target)
    {
        var targetRecipient = EnsureComp<MindLinkRecipientComponent>(target);
        targetRecipient.Initiators.Add(source.Owner);
        if (!source.Comp.TwoWayCommunication)
            return;

        targetRecipient.ReplyInitiators.Add(source.Owner);

        // Recipients get this component temporarily so that their reply UI has an entity to live on.
        if (!HasComp<MindLinkComponent>(target))
            AddComp(target, new MindLinkComponent { TemporaryUiHost = true });

        if (targetRecipient.ReplyAction is null || Deleted(targetRecipient.ReplyAction.Value))
            targetRecipient.ReplyAction = _actions.AddAction(target, ReplyActionPrototype, target);
    }

    private void ClearAllConnections(EntityUid uid)
    {
        if (TryComp(uid, out MindLinkComponent? link))
            ClearConnections((uid, link));

        ClearIncomingConnections(uid);
    }

    private void ClearConnections(Entity<MindLinkComponent> source)
    {
        foreach (var target in source.Comp.Targets.ToArray())
            ClearConnection(source, target);
    }

    private void ClearIncomingConnections(
        EntityUid target,
        MindLinkRecipientComponent? recipient = null,
        bool removeRecipient = true)
    {
        if (!Resolve(target, ref recipient, false))
            return;

        foreach (var initiator in recipient.Initiators.ToArray())
        {
            if (TryComp(initiator, out MindLinkComponent? source)
                && source.Targets.Contains(target))
                ClearConnection((initiator, source), target, removeRecipient);
            else
            {
                recipient.Initiators.Remove(initiator);
                recipient.ReplyInitiators.Remove(initiator);
            }
        }
    }

    private void ClearConnection(Entity<MindLinkComponent> source, EntityUid target, bool removeRecipient = true)
    {
        if (!source.Comp.Targets.Remove(target))
            return;

        source.Comp.PendingTargets.Remove(target);

        if (TryComp(target, out MindLinkRecipientComponent? recipient))
        {
            recipient.Initiators.Remove(source.Owner);
            recipient.ReplyInitiators.Remove(source.Owner);

            if (recipient.ReplyInitiators.Count == 0 && recipient.ReplyAction is { } action)
            {
                _actions.RemoveAction(target, action);
                recipient.ReplyAction = null;
            }

            if (removeRecipient && recipient.Initiators.Count == 0)
            {
                RemComp<MindLinkRecipientComponent>(target);

                if (TryComp(target, out MindLinkComponent? targetLink) && targetLink.TemporaryUiHost)
                    RemComp<MindLinkComponent>(target);
            }
        }
    }

    private bool IsValidTarget(EntityUid uid)
    {
        return TryComp(uid, out MobStateComponent? mob)
               && mob.CurrentState == MobState.Alive
               && _players.TryGetSessionByEntity(uid, out _)
               && !HasComp<PossessionImmuneComponent>(uid)
               && HasActiveBrain(uid);
    }

    private bool IsWithinRange(EntityUid source, EntityUid target, float range)
    {
        if (range < 0f)
            return true;

        return Transform(source).Coordinates.TryDistance(EntityManager, Transform(target).Coordinates, out var distance)
               && distance <= range;
    }

    private bool IsActiveConnection(EntityUid source, EntityUid target)
    {
        return IsValidTarget(source)
               && IsValidTarget(target)
               && TryComp(source, out MindLinkComponent? link)
               && link.Targets.Contains(target);
    }

    private bool HasActiveBrain(EntityUid uid, EntityUid? excludedBrain = null)
    {
        if (uid != excludedBrain && TryComp(uid, out BrainComponent? brain))
            return brain.Active;

        if (!TryComp(uid, out BodyComponent? body))
            return false;

        foreach (var (organ, _) in _body.GetBodyOrgans(uid, body))
        {
            if (organ == excludedBrain)
                continue;

            if (TryComp(organ, out brain) && brain.Active)
                return true;
        }

        return false;
    }

    private bool TrySendMessage(EntityUid sender, List<EntityUid> recipients, string text)
    {
        if (!_players.TryGetSessionByEntity(sender, out var senderSession))
            return false;

        if (_chat.HandleRateLimit(senderSession) != RateLimitStatus.Allowed)
            return false;

        var escapedText = FormattedMessage.EscapeText(text);
        var recipientName = recipients.Count == 1
            ? FormattedMessage.EscapeText(Name(recipients[0]))
            : Loc.GetString("mind-link-all-targets");
        var senderName = FormattedMessage.EscapeText(Name(sender));
        var senderMessage = Loc.GetString("mind-link-chat-sent", ("target", recipientName), ("message", escapedText));
        _chat.ChatMessageToOne(ChatChannel.Telepathic, escapedText, senderMessage, sender, false,
            senderSession.Channel, Color.PaleVioletRed, recordReplay: true, author: senderSession.UserId);

        foreach (var recipient in recipients)
        {
            if (!_players.TryGetSessionByEntity(recipient, out var recipientSession))
                continue;

            var recipientMessage = Loc.GetString("mind-link-chat-received", ("source", senderName), ("message", escapedText));
            _chat.ChatMessageToOne(ChatChannel.Telepathic, escapedText, recipientMessage, sender, false,
                recipientSession.Channel, Color.PaleVioletRed, author: senderSession.UserId);
            _adminLogger.Add(LogType.Chat, LogImpact.Low,
                $"Mind link from {ToPrettyString(sender):user} to {ToPrettyString(recipient):user}: {text}");
        }
        return true;
    }
}
