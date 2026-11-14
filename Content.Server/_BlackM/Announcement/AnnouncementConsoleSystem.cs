using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Server.Popups;
using Content.Server.Radio.Components;
using Content.Shared._BlackM.Announcement;
using Content.Shared.Access;
using Content.Shared.Access.Systems;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Ghost;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.Radio.Components;
using Content.Shared.Roles.Jobs;
using Robust.Server.GameObjects;
using Robust.Server.GameStates;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._BlackM.Announcement;

public sealed class AnnouncementConsoleSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly SharedIdCardSystem _idCard = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedJobSystem _jobs = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly PvsOverrideSystem _pvs = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    private static readonly TimeSpan DeliveryDelay = TimeSpan.FromSeconds(0.35);
    private const float PvsExtraSeconds = 8f;
    private const float PvsMaxSeconds = 45f;
    private const string EarsSlot = "ears";

    private readonly Dictionary<(EntityUid Speaker, ICommonSession Session), TimeSpan> _overrides = new();
    private readonly List<(EntityUid Speaker, ICommonSession Session)> _expired = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnnouncementConsoleComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<AnnouncementConsoleComponent, AnnouncementConsoleSendMessage>(OnSend);

        _player.PlayerStatusChanged += OnPlayerStatusChanged;
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _player.PlayerStatusChanged -= OnPlayerStatusChanged;
        _overrides.Clear();
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (e.NewStatus != SessionStatus.Disconnected)
            return;

        foreach (var key in _overrides.Keys.Where(k => k.Session == e.Session).ToList())
        {
            _overrides.Remove(key);
        }
    }

    private void OnUiOpened(Entity<AnnouncementConsoleComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (!AnnouncementConsoleUiKey.Key.Equals(args.UiKey))
            return;

        UpdateUi(ent, args.Actor);
    }

    private void UpdateUi(Entity<AnnouncementConsoleComponent> ent, EntityUid actor)
    {
        var hasAccess = _accessReader.IsAllowed(actor, ent.Owner);
        var tags = _accessReader.FindAccessTags(actor);

        var channels = new List<AnnouncementChannelEntry>();
        foreach (var id in ent.Comp.Channels)
        {
            if (!_proto.TryIndex(id, out var proto))
                continue;

            var allowed = hasAccess && (ent.Comp.IgnoreChannelAccess || HasChannelAccess(proto, tags));
            channels.Add(new AnnouncementChannelEntry(proto.ID, Loc.GetString(proto.Name), proto.Accent, allowed));
        }

        GetSenderInfo(actor, out var name, out var job);

        _ui.SetUiState(ent.Owner, AnnouncementConsoleUiKey.Key,
            new AnnouncementConsoleBuiState(channels, ent.Comp.NextAnnouncement, ent.Comp.MaxLength, name, job, hasAccess));
    }

    private static bool HasChannelAccess(AnnouncementChannelPrototype channel, ICollection<ProtoId<AccessLevelPrototype>> tags)
    {
        if (channel.RequiredAccess.Count == 0)
            return true;

        foreach (var access in channel.RequiredAccess)
        {
            if (tags.Contains(access))
                return true;
        }

        return false;
    }

    private void GetSenderInfo(EntityUid user, out string name, out string job)
    {
        name = Identity.Name(user, EntityManager);
        job = string.Empty;

        if (_idCard.TryFindIdCard(user, out var id))
        {
            if (!string.IsNullOrWhiteSpace(id.Comp.FullName))
                name = id.Comp.FullName;

            if (!string.IsNullOrWhiteSpace(id.Comp.LocalizedJobTitle))
                job = id.Comp.LocalizedJobTitle;
        }

        if (job.Length == 0 && _mind.TryGetMind(user, out var mindId, out _) && _jobs.MindTryGetJobName(mindId, out var jobName))
            job = jobName;
    }

    private void OnSend(Entity<AnnouncementConsoleComponent> ent, ref AnnouncementConsoleSendMessage args)
    {
        var user = args.Actor;
        if (!user.Valid)
            return;

        if (_timing.CurTime < ent.Comp.NextAnnouncement)
        {
            var left = (int) Math.Ceiling((ent.Comp.NextAnnouncement - _timing.CurTime).TotalSeconds);
            _popup.PopupEntity(Loc.GetString("blackm-announce-popup-cooldown", ("seconds", left)), ent.Owner, user);
            return;
        }

        if (!_accessReader.IsAllowed(user, ent.Owner))
        {
            _popup.PopupEntity(Loc.GetString("blackm-announce-popup-denied"), ent.Owner, user);
            return;
        }

        var channelId = args.Channel;
        if (!ent.Comp.Channels.Any(c => c.Id == channelId) ||
            !_proto.TryIndex<AnnouncementChannelPrototype>(channelId, out var channel))
        {
            return;
        }

        if (!ent.Comp.IgnoreChannelAccess && !HasChannelAccess(channel, _accessReader.FindAccessTags(user)))
        {
            _popup.PopupEntity(Loc.GetString("blackm-announce-popup-channel-denied"), ent.Owner, user);
            return;
        }

        var text = SanitizeText(args.Text, ent.Comp.MaxLength);
        if (text.Length == 0)
            return;

        ent.Comp.NextAnnouncement = _timing.CurTime + ent.Comp.Cooldown;

        GetSenderInfo(user, out var name, out var job);
        var sender = job.Length == 0
            ? name
            : Loc.GetString("blackm-announce-sender", ("name", name), ("job", job));
        var channelName = Loc.GetString(channel.Name);
        var title = Loc.GetString(channel.Title, ("channel", channelName));
        var duration = ent.Comp.Duration > 0f
            ? ent.Comp.Duration
            : Math.Clamp(4f + text.Length / 20f, 5f, 15f);

        var recipients = new List<ICommonSession>();
        foreach (var session in _player.Sessions)
        {
            if (session.Status != SessionStatus.InGame || session.AttachedEntity is not { } mob)
                continue;

            if (mob == user || CanReceive(mob, channel))
                recipients.Add(session);
        }

        var pvsSeconds = Math.Min(PvsMaxSeconds, duration + text.Length / 25f + PvsExtraSeconds);
        var until = _timing.CurTime + TimeSpan.FromSeconds(pvsSeconds);
        foreach (var session in recipients)
        {
            if (session.AttachedEntity == user)
                continue;

            var key = (user, session);
            if (_overrides.TryGetValue(key, out var existing))
            {
                if (existing < until)
                    _overrides[key] = until;
                continue;
            }

            _pvs.AddSessionOverride(user, session);
            _overrides[key] = until;
        }

        var ev = new AnnouncementBroadcastEvent(GetNetEntity(user), sender, title, text, channel.Accent, duration);
        var filter = Filter.Empty().AddPlayers(recipients);
        Timer.Spawn(DeliveryDelay, () => RaiseNetworkEvent(ev, filter));

        var wrapped = Loc.GetString("blackm-announce-chat-wrap",
            ("color", channel.Accent.ToHex()),
            ("channel", FormattedMessage.EscapeText(channelName)),
            ("sender", FormattedMessage.EscapeText(sender)),
            ("message", FormattedMessage.EscapeText(text)));
        _chat.ChatMessageToMany(ChatChannel.Radio, text, wrapped, user, false, true,
            recipients.Select(s => s.Channel));

        _adminLog.Add(LogType.Chat, LogImpact.Medium,
            $"{ToPrettyString(user):user} made an announcement on channel {channel.ID} via {ToPrettyString(ent.Owner):entity}: {text}");

        _popup.PopupEntity(Loc.GetString("blackm-announce-popup-sent", ("channel", channelName)), ent.Owner, user);
        UpdateUi(ent, user);
    }

    private static string SanitizeText(string raw, int maxLength)
    {
        var text = raw.Replace("\r", string.Empty).Trim();

        var lines = text.Split('\n')
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .Take(6);
        text = string.Join("\n", lines);

        if (text.Length > maxLength)
            text = text[..maxLength].TrimEnd();

        return text;
    }

    private bool CanReceive(EntityUid mob, AnnouncementChannelPrototype channel)
    {
        if (channel.Everyone || HasComp<GhostComponent>(mob))
            return true;

        if (_mind.TryGetMind(mob, out var mindId, out _) &&
            _jobs.MindTryGetJobId(mindId, out var jobId) &&
            jobId != null)
        {
            if (channel.Jobs.Contains(jobId.Value))
                return true;

            foreach (var depId in channel.Departments)
            {
                if (_proto.TryIndex(depId, out var department) && department.Roles.Contains(jobId.Value))
                    return true;
            }
        }

        if (channel.RadioChannels.Count == 0)
            return false;

        if (TryComp<ActiveRadioComponent>(mob, out var radio) &&
            (radio.ReceiveAllChannels || channel.RadioChannels.Any(c => radio.Channels.Contains(c.Id))))
        {
            return true;
        }

        if (_inventory.TryGetSlotEntity(mob, EarsSlot, out var ears) &&
            TryComp<EncryptionKeyHolderComponent>(ears, out var keys) &&
            channel.RadioChannels.Any(c => keys.Channels.Contains(c.Id)))
        {
            return true;
        }

        return false;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_overrides.Count == 0)
            return;

        var now = _timing.CurTime;
        foreach (var (key, until) in _overrides)
        {
            if (until <= now)
                _expired.Add(key);
        }

        foreach (var key in _expired)
        {
            _overrides.Remove(key);

            if (key.Session.Status == SessionStatus.Disconnected || TerminatingOrDeleted(key.Speaker))
                continue;

            if (key.Session.AttachedEntity == key.Speaker)
                continue;

            _pvs.RemoveSessionOverride(key.Speaker, key.Session);
        }

        _expired.Clear();
    }
}
