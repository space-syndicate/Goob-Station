using Content.Server.Actions;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Shared._CorvaxGoob.MindLink;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Players.RateLimiting;
using Robust.Server.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

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
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<MindLinkComponent, ComponentInit>(OnMindLinkInit);
        SubscribeLocalEvent<MindLinkComponent, ComponentShutdown>(OnMindLinkShutdown);
        SubscribeLocalEvent<MindLinkComponent, OpenMindLinkEvent>(OnOpenAction);
        SubscribeLocalEvent<MindLinkRecipientComponent, ReplyMindLinkEvent>(OnReplyAction);

        Subs.BuiEvents<MindLinkComponent>(MindLinkUiKey.Key, subs =>
        {
            subs.Event<SelectMindLinkTargetMessage>(OnTargetSelected);
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
        ClearConnection(ent);

        if (ent.Comp.AddedUserInterface)
            RemComp<UserInterfaceComponent>(ent);
    }

    private void OnOpenAction(Entity<MindLinkComponent> ent, ref OpenMindLinkEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        ent.Comp.TwoWayCommunication = args.TwoWayCommunication;
        ent.Comp.Range = args.Range;
        ent.Comp.PendingReplyTarget = null;
        ent.Comp.SelectingReplyTarget = false;
        OpenTargetSelection(ent);
    }

    private void OnReplyAction(Entity<MindLinkRecipientComponent> ent, ref ReplyMindLinkEvent args)
    {
        if (args.Handled || ent.Comp.Initiators.Count == 0
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
            OpenMessageWindow(ent.Owner, initiator, isReply: true);
            return;
        }

        link.SelectingReplyTarget = true;
        OpenReplyTargetSelection(ent.Owner, initiators);
    }

    private void OnTargetSelected(Entity<MindLinkComponent> ent, ref SelectMindLinkTargetMessage args)
    {
        var target = GetEntity(args.Target);
        if (ent.Comp.SelectingReplyTarget)
        {
            if (!TryComp(ent.Owner, out MindLinkRecipientComponent? recipient)
                || !recipient.Initiators.Contains(target)
                || !IsActiveConnection(target, ent.Owner))
            {
                OpenReplyTargetSelection(ent.Owner, GetReplyTargets(ent.Owner));
                return;
            }

            ent.Comp.SelectingReplyTarget = false;
            ent.Comp.PendingReplyTarget = target;
            OpenMessageWindow(ent.Owner, target, isReply: true);
            return;
        }

        var isCurrentTarget = ent.Comp.CurrentTarget == target;
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
        OpenMessageWindow(ent.Owner, target, isReply: false);
    }

    private void OnMessageSent(Entity<MindLinkComponent> ent, ref SendMindLinkMessage args)
    {
        if (args.Actor != ent.Owner)
            return;

        var message = args.Message.Trim();
        var source = args.Actor;
        var isReply = ent.Comp.PendingReplyTarget is not null;
        var target = ent.Comp.PendingReplyTarget ?? ent.Comp.CurrentTarget;

        if (isReply)
        {
            if (target is not { } replyTarget
                || !TryComp(ent.Owner, out MindLinkRecipientComponent? recipient)
                || !recipient.Initiators.Contains(replyTarget)
                || !IsActiveConnection(replyTarget, ent.Owner))
                return;
        }

        if (target is null || !IsActiveConnection(source, target.Value)
            || message.Length == 0 || message.Length > MaxMessageLength)
            return;

        if (!TrySendMessage(source, target.Value, message))
            return;

        ent.Comp.PendingReplyTarget = null;

        // A reply action is earned by receiving the first message, not merely by
        // appearing in the source's target picker.
        if (!isReply && ent.Comp.TwoWayCommunication)
            GrantReplyAction(ent, target.Value);
    }

    private void OpenTargetSelection(Entity<MindLinkComponent> ent)
    {
        var targets = GetTargets(ent);
        _ui.OpenUi(ent.Owner, MindLinkUiKey.Key, ent.Owner);
        _ui.SetUiState(ent.Owner, MindLinkUiKey.Key, new MindLinkBuiState(targets, null, false));
    }

    private void OpenReplyTargetSelection(EntityUid recipient, List<MindLinkTarget> targets)
    {
        _ui.OpenUi(recipient, MindLinkUiKey.Key, recipient);
        _ui.SetUiState(recipient, MindLinkUiKey.Key, new MindLinkBuiState(targets, null, true));
    }

    private void OpenMessageWindow(EntityUid uiOwner, EntityUid recipient, bool isReply)
    {
        _ui.OpenUi(uiOwner, MindLinkUiKey.Key, uiOwner);
        _ui.SetUiState(uiOwner, MindLinkUiKey.Key,
            new MindLinkBuiState([], Name(recipient), isReply));
    }

    private List<MindLinkTarget> GetTargets(Entity<MindLinkComponent> source)
    {
        var result = new List<MindLinkTarget>();

        // An established connection is always shown first and does not depend on range.
        if (source.Comp.CurrentTarget is { } current && IsActiveConnection(source.Owner, current))
            result.Add(new MindLinkTarget(GetNetEntity(current), Name(current)));

        var available = new List<MindLinkTarget>();
        var query = EntityQueryEnumerator<MobStateComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            if (uid != source.Owner
                && uid != source.Comp.CurrentTarget
                && IsValidTarget(uid)
                && IsWithinRange(source.Owner, uid, source.Comp.Range))
                available.Add(new MindLinkTarget(GetNetEntity(uid), Name(uid)));
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

        foreach (var initiator in recipientComp.Initiators)
        {
            if (IsActiveConnection(initiator, recipient))
                result.Add(new MindLinkTarget(GetNetEntity(initiator), Name(initiator)));
        }

        result.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase));
        return result;
    }

    private void SetTarget(Entity<MindLinkComponent> source, EntityUid target)
    {
        ClearConnection(source);
        source.Comp.CurrentTarget = target;
        Dirty(source);
    }

    private void GrantReplyAction(Entity<MindLinkComponent> source, EntityUid target)
    {
        // Recipients get this component temporarily so that their reply UI has an entity to live on.
        if (!HasComp<MindLinkComponent>(target))
            AddComp(target, new MindLinkComponent { TemporaryUiHost = true });

        var targetRecipient = EnsureComp<MindLinkRecipientComponent>(target);
        targetRecipient.Initiators.Add(source.Owner);
        if (targetRecipient.ReplyAction is null || Deleted(targetRecipient.ReplyAction.Value))
            targetRecipient.ReplyAction = _actions.AddAction(target, ReplyActionPrototype, target);
    }

    public override void Update(float frameTime)
    {
        var brokenConnections = new List<Entity<MindLinkComponent>>();
        var query = EntityQueryEnumerator<MindLinkComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.CurrentTarget is { } target
                && !IsActiveConnection(uid, target))
                brokenConnections.Add((uid, component));
        }

        foreach (var connection in brokenConnections)
            ClearConnection(connection);
    }

    private void ClearConnection(Entity<MindLinkComponent> source)
    {
        if (source.Comp.CurrentTarget is not { } target)
            return;

        if (TryComp(target, out MindLinkRecipientComponent? recipient)
            && recipient.Initiators.Remove(source.Owner)
            && recipient.Initiators.Count == 0)
        {
            if (recipient.ReplyAction is { } action)
                _actions.RemoveAction(target, action);
            RemComp<MindLinkRecipientComponent>(target);

            if (TryComp(target, out MindLinkComponent? targetLink) && targetLink.TemporaryUiHost)
                RemComp<MindLinkComponent>(target);
        }

        source.Comp.CurrentTarget = null;
        Dirty(source);
    }

    private bool IsValidTarget(EntityUid uid)
    {
        return TryComp(uid, out MobStateComponent? mob)
               && mob.CurrentState == MobState.Alive
               && _players.TryGetSessionByEntity(uid, out _)
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
               && link.CurrentTarget == target;
    }

    private bool HasActiveBrain(EntityUid uid)
    {
        if (TryComp(uid, out BrainComponent? brain))
            return brain.Active;

        if (!TryComp(uid, out BodyComponent? body))
            return false;

        foreach (var (organ, _) in _body.GetBodyOrgans(uid, body))
        {
            if (TryComp(organ, out brain) && brain.Active)
                return true;
        }

        return false;
    }

    private bool TrySendMessage(EntityUid sender, EntityUid recipient, string text)
    {
        if (!_players.TryGetSessionByEntity(sender, out var senderSession)
            || !_players.TryGetSessionByEntity(recipient, out var recipientSession))
            return false;

        if (_chat.HandleRateLimit(senderSession) != RateLimitStatus.Allowed)
            return false;

        var senderName = Name(sender);
        var recipientName = Name(recipient);
        var escapedText = FormattedMessage.EscapeText(text);
        var senderMessage = Loc.GetString("mind-link-chat-sent", ("target", recipientName), ("message", escapedText));
        var recipientMessage = Loc.GetString("mind-link-chat-received", ("source", senderName), ("message", escapedText));

        _chat.ChatMessageToOne(ChatChannel.Telepathic, escapedText, senderMessage, sender, false,
            senderSession.Channel, Color.PaleVioletRed, recordReplay: true, author: senderSession.UserId);
        _chat.ChatMessageToOne(ChatChannel.Telepathic, escapedText, recipientMessage, sender, false,
            recipientSession.Channel, Color.PaleVioletRed, author: senderSession.UserId);
        _adminLogger.Add(LogType.Chat, LogImpact.Low,
            $"Mind link from {ToPrettyString(sender):user} to {ToPrettyString(recipient):user}: {text}");
        return true;
    }
}
