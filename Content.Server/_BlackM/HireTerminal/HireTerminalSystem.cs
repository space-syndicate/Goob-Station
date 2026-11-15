using Content.Server._BlackM.Passport;
using Content.Server.Chat.Systems;
using Content.Shared.Chat;
using Content.Server.GameTicking;
using Content.Shared.GameTicking;
using Content.Server.Hands.Systems;
using Content.Server.Popups;
using Content.Server.Station.Systems;
using Content.Shared._BlackM.HireTerminal;
using Content.Shared._BlackM.Passport;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Roles;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using System.Diagnostics.CodeAnalysis;

namespace Content.Server._BlackM.HireTerminal;

public sealed class HireTerminalSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem     _itemSlots = default!;
    [Dependency] private readonly UserInterfaceSystem _ui        = default!;
    [Dependency] private readonly SharedAudioSystem   _audio     = default!;
    [Dependency] private readonly PopupSystem         _popup     = default!;
    [Dependency] private readonly HandsSystem         _hands     = default!;
    [Dependency] private readonly StationSystem       _station   = default!;
    [Dependency] private readonly PassportSystem      _passport  = default!;
    [Dependency] private readonly GameTicker          _ticker    = default!;
    [Dependency] private readonly MobStateSystem      _mob       = default!;
    [Dependency] private readonly IPrototypeManager   _proto     = default!;
    [Dependency] private readonly IPlayerManager      _players   = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly ChatSystem          _chat       = default!;
    [Dependency] private readonly IGameTiming         _timing    = default!;

    private static readonly SoundSpecifier SuccessSound = new SoundPathSpecifier("/Audio/_BlackM/Announcements/terminal_success.ogg");
    private static readonly SoundSpecifier ErrorSound   = new SoundPathSpecifier("/Audio/_BlackM/Announcements/terminal_alert.ogg");
    private static readonly SoundSpecifier SubmitSound  = new SoundPathSpecifier("/Audio/_BlackM/Announcements/terminal_request.ogg");

    private enum AppStatus : byte { Pending, Approved, Rejected }

    private sealed class Application
    {
        public int Id;
        public string PassportNumber = string.Empty;
        public string ApplicantName  = string.Empty;
        public string CurrentJob     = string.Empty;
        public string PositionId     = string.Empty;
        public string Comment        = string.Empty;
        public string Time           = string.Empty;
        public AppStatus Status      = AppStatus.Pending;
        public string ReviewedBy     = string.Empty;
    }

    private readonly List<Application> _applications = new();
    private readonly List<HireLogEntry> _log = new();
    private readonly HashSet<string> _issuedNumbers = new();
    private int _nextId = 1;
    private TimeSpan _nextRefresh;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HireTerminalComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<HireTerminalComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<HireTerminalComponent, EntRemovedFromContainerMessage>(OnRemoved);

        SubscribeLocalEvent<HireTerminalComponent, HireTerminalSubmitMessage>(OnSubmit);
        SubscribeLocalEvent<HireTerminalComponent, HireTerminalClaimMessage>(OnClaim);
        SubscribeLocalEvent<HireTerminalComponent, HireTerminalEjectMessage>(OnEject);
        SubscribeLocalEvent<HireTerminalComponent, HireTerminalApproveMessage>(OnApprove);
        SubscribeLocalEvent<HireTerminalComponent, HireTerminalRejectMessage>(OnReject);

        SubscribeLocalEvent<RoundRestartCleanupEvent>(_ => Reset());
    }

    private void Reset()
    {
        _applications.Clear();
        _log.Clear();
        _issuedNumbers.Clear();
        _nextId = 1;
    }

    public override void Update(float frameTime)
    {
        if (_timing.CurTime < _nextRefresh)
            return;

        _nextRefresh = _timing.CurTime + TimeSpan.FromSeconds(3);

        var query = EntityQueryEnumerator<HireTerminalComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_ui.IsUiOpen(uid, HireTerminalUiKey.Key))
                UpdateUi(uid, comp);
        }
    }

    #region Events

    private void OnUiOpened(EntityUid uid, HireTerminalComponent comp, BoundUIOpenedEvent args)
        => UpdateUi(uid, comp);

    private void OnInserted(EntityUid uid, HireTerminalComponent comp, EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != comp.SlotId)
            return;

        _appearance.SetData(uid, HireTerminalVisuals.HasPassport, true);
        UpdateUi(uid, comp);
    }

    private void OnRemoved(EntityUid uid, HireTerminalComponent comp, EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != comp.SlotId)
            return;

        _appearance.SetData(uid, HireTerminalVisuals.HasPassport, false);
        UpdateUi(uid, comp);
    }

    private void OnEject(EntityUid uid, HireTerminalComponent comp, HireTerminalEjectMessage args)
    {
        if (_itemSlots.TryGetSlot(uid, comp.SlotId, out var slot))
            _itemSlots.TryEjectToHands(uid, slot, args.Actor);

        UpdateUi(uid, comp);
    }

    private void OnSubmit(EntityUid uid, HireTerminalComponent comp, HireTerminalSubmitMessage args)
    {
        if (!TryGetBoundPassport(uid, comp, args.Actor, out var passportUid, out var passport))
            return;

        if (HasAdditionalJob(passport))
        {
            Error(uid, args.Actor, "hire-terminal-popup-already-has");
            return;
        }

        if (!_proto.TryIndex<AdditionalPositionPrototype>(args.PositionId, out var position))
        {
            Error(uid, args.Actor, "hire-terminal-popup-no-position");
            return;
        }

        var existing = FindApplication(passport.PassportNumber);
        if (existing is { Status: AppStatus.Pending or AppStatus.Approved })
        {
            Error(uid, args.Actor, "hire-terminal-popup-already-pending");
            return;
        }

        if (existing != null)
            _applications.Remove(existing);

        var comment = (args.Comment ?? string.Empty).Trim();
        if (comment.Length > comp.MaxCommentLength)
            comment = comment[..comp.MaxCommentLength];

        var app = new Application
        {
            Id             = _nextId++,
            PassportNumber = passport.PassportNumber,
            ApplicantName  = FullName(passport),
            CurrentJob     = passport.JobTitle,
            PositionId     = position.ID,
            Comment        = comment,
            Time           = Now(),
            Status         = AppStatus.Pending,
        };
        _applications.Add(app);

        if (NeedsReview(uid, comp))
        {
            _audio.PlayPvs(SubmitSound, uid);
            _chat.TrySendInGameICMessage(
                uid,
                Loc.GetString("hire-terminal-speech-submitted"),
                InGameICChatType.Speak,
                hideChat: false,
                ignoreActionBlocker: true);
            RefreshAll();
            return;
        }

        IssueKey(uid, comp, passportUid, passport, app, position, args.Actor,
            Loc.GetString("hire-terminal-log-auto"));
    }

    private void OnClaim(EntityUid uid, HireTerminalComponent comp, HireTerminalClaimMessage args)
    {
        if (!TryGetBoundPassport(uid, comp, args.Actor, out var passportUid, out var passport))
            return;

        if (HasAdditionalJob(passport))
        {
            Error(uid, args.Actor, "hire-terminal-popup-already-has");
            return;
        }

        var app = FindApplication(passport.PassportNumber);
        if (app == null || app.Status == AppStatus.Rejected)
        {
            Error(uid, args.Actor, "hire-terminal-popup-no-application");
            return;
        }

        var approvedBy = app.ReviewedBy;

        if (app.Status == AppStatus.Pending)
        {
            if (NeedsReview(uid, comp))
            {
                Error(uid, args.Actor, "hire-terminal-popup-still-pending");
                return;
            }

            approvedBy = Loc.GetString("hire-terminal-log-auto");
        }

        if (!_proto.TryIndex<AdditionalPositionPrototype>(app.PositionId, out var position))
        {
            _applications.Remove(app);
            Error(uid, args.Actor, "hire-terminal-popup-no-position");
            UpdateUi(uid, comp);
            return;
        }

        IssueKey(uid, comp, passportUid, passport, app, position, args.Actor, approvedBy);
    }

    private void OnApprove(EntityUid uid, HireTerminalComponent comp, HireTerminalApproveMessage args)
    {
        if (!TryGetReviewable(uid, comp, args.Actor, args.ApplicationId, out var app))
            return;

        app.Status     = AppStatus.Approved;
        app.ReviewedBy = MetaData(args.Actor).EntityName;
        EntityManager.System<Content.Server._BlackM.PersonalObjectives.PersonalObjectivesSystem>().AddProgress(args.Actor, Content.Shared._BlackM.PersonalObjectives.BlackMPersonalCounters.HiresApproved); // personal objectives

        _audio.PlayPvs(SuccessSound, uid);
        _popup.PopupEntity(Loc.GetString("hire-terminal-popup-approved", ("name", app.ApplicantName)), uid, args.Actor);
        RefreshAll();
    }

    private void OnReject(EntityUid uid, HireTerminalComponent comp, HireTerminalRejectMessage args)
    {
        if (!TryGetReviewable(uid, comp, args.Actor, args.ApplicationId, out var app))
            return;

        app.Status     = AppStatus.Rejected;
        app.ReviewedBy = MetaData(args.Actor).EntityName;

        _audio.PlayPvs(ErrorSound, uid);
        _popup.PopupEntity(Loc.GetString("hire-terminal-popup-rejected", ("name", app.ApplicantName)), uid, args.Actor);
        RefreshAll();
    }

    #endregion

    #region Logic

    private bool TryGetReviewable(EntityUid uid, HireTerminalComponent comp, EntityUid actor, int id,
        [NotNullWhen(true)] out Application? app)
    {
        app = null;

        if (!IsLeader(uid, comp, actor))
        {
            Error(uid, actor, "hire-terminal-popup-not-leader");
            return false;
        }

        app = _applications.Find(a => a.Id == id && a.Status == AppStatus.Pending);
        if (app == null)
        {
            UpdateUi(uid, comp);
            return false;
        }

        return true;
    }

    private void IssueKey(EntityUid uid, HireTerminalComponent comp, EntityUid passportUid,
        PassportComponent passport, Application app, AdditionalPositionPrototype position,
        EntityUid actor, string approvedBy)
    {
        var positionName = Loc.GetString(position.Name);

        if (!_passport.TrySetAdditionalJob(passportUid, position.ID, positionName, passport))
        {
            Error(uid, actor, "hire-terminal-popup-already-has");
            return;
        }

        _issuedNumbers.Add(app.PassportNumber);
        _applications.Remove(app);

        var badge = Spawn(position.Badge, Transform(uid).Coordinates);
        _hands.TryPickupAnyHand(actor, badge);

        _log.Add(new HireLogEntry(
            Now(),
            app.ApplicantName,
            positionName,
            Loc.GetString(position.Department),
            approvedBy,
            position.Color));

        while (_log.Count > Math.Max(1, comp.MaxLogEntries))
            _log.RemoveAt(0);

        _audio.PlayPvs(SuccessSound, uid);
        _popup.PopupEntity(Loc.GetString("hire-terminal-popup-issued", ("position", positionName)), uid, actor);

        if (_itemSlots.TryGetSlot(uid, comp.SlotId, out var slot))
            _itemSlots.TryEjectToHands(uid, slot, actor);

        RefreshAll();
    }

    private bool NeedsReview(EntityUid terminal, HireTerminalComponent comp)
        => NeedsReview(comp, GetLeaders(terminal, comp));

    private bool NeedsReview(HireTerminalComponent comp, List<EntityUid> leaders)
    {
        if (leaders.Count == 0)
            return false;

        if (comp.AutoApprovePlayerThreshold > 0 && _players.PlayerCount < comp.AutoApprovePlayerThreshold)
            return false;

        return true;
    }

    private bool IsLeader(EntityUid terminal, HireTerminalComponent comp, EntityUid actor)
    {
        if (_mob.IsDead(actor))
            return false;

        var job = GetJobId(actor);
        return job != null && IsLeaderJob(comp, job) && SameStation(terminal, actor);
    }

    private List<EntityUid> GetLeaders(EntityUid terminal, HireTerminalComponent comp)
    {
        var result = new List<EntityUid>();

        var query = EntityQueryEnumerator<MindContainerComponent, ActorComponent>();
        while (query.MoveNext(out var ent, out var mindContainer, out _))
        {
            if (!mindContainer.HasMind || _mob.IsDead(ent))
                continue;

            var job = GetJobId(ent);
            if (job == null || !IsLeaderJob(comp, job))
                continue;

            if (!SameStation(terminal, ent))
                continue;

            result.Add(ent);
        }

        return result;
    }

    private static bool IsLeaderJob(HireTerminalComponent comp, string job)
    {
        foreach (var id in comp.LeaderJobIds)
        {
            if (string.Equals(id, job, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private bool SameStation(EntityUid a, EntityUid b)
    {
        var stationA = _station.GetOwningStation(a);
        var stationB = _station.GetOwningStation(b);

        if (stationA == null || stationB == null)
            return Transform(a).MapID == Transform(b).MapID;

        return stationA == stationB;
    }

    private string? GetJobId(EntityUid mob)
    {
        if (!TryComp<MindContainerComponent>(mob, out var mindContainer) || !mindContainer.HasMind)
            return null;

        if (!TryComp<MindComponent>(mindContainer.Mind!.Value, out var mind))
            return null;

        foreach (var roleEnt in mind.MindRoles)
        {
            if (TryComp<MindRoleComponent>(roleEnt, out var roleComp) && roleComp.JobPrototype != null)
                return roleComp.JobPrototype;
        }

        return null;
    }

    private Application? FindApplication(string passportNumber)
        => _applications.Find(a => a.PassportNumber == passportNumber);

    private bool HasAdditionalJob(PassportComponent passport)
        => !string.IsNullOrEmpty(passport.AdditionalJobId) || _issuedNumbers.Contains(passport.PassportNumber);

    private static string FullName(PassportComponent passport)
        => $"{passport.Surname} {passport.OwnerName}".Trim();

    private bool IsOwner(EntityUid actor, PassportComponent passport)
    {
        var name = MetaData(actor).EntityName;
        var absent = Loc.GetString("passport-surname-absent");

        var expected = string.IsNullOrWhiteSpace(passport.Surname) || passport.Surname == absent
            ? passport.OwnerName
            : $"{passport.OwnerName} {passport.Surname}";

        return string.Equals(name, expected, StringComparison.OrdinalIgnoreCase);
    }

    private string Now() => _ticker.RoundDuration().ToString(@"hh\:mm");

    private void Error(EntityUid terminal, EntityUid actor, string locKey)
    {
        _audio.PlayPvs(ErrorSound, terminal);
        _popup.PopupEntity(Loc.GetString(locKey), terminal, actor);
    }

    private bool TryGetPassport(EntityUid uid, HireTerminalComponent comp,
        out EntityUid passportUid, [NotNullWhen(true)] out PassportComponent? passport)
    {
        passportUid = default;
        passport = null;

        if (!_itemSlots.TryGetSlot(uid, comp.SlotId, out var slot) || slot.Item is not { } item)
            return false;

        passportUid = item;
        return TryComp(item, out passport);
    }

    private bool TryGetBoundPassport(EntityUid uid, HireTerminalComponent comp, EntityUid actor,
        out EntityUid passportUid, [NotNullWhen(true)] out PassportComponent? passport)
    {
        if (!TryGetPassport(uid, comp, out passportUid, out passport))
        {
            Error(uid, actor, "hire-terminal-popup-no-passport");
            return false;
        }

        if (!passport.IsBound)
        {
            Error(uid, actor, "hire-terminal-popup-unbound");
            return false;
        }

        if (comp.RequireOwner && !IsOwner(actor, passport))
        {
            Error(uid, actor, "hire-terminal-popup-not-owner");
            return false;
        }

        return true;
    }

    #endregion

    #region UI

    private void RefreshAll()
    {
        var query = EntityQueryEnumerator<HireTerminalComponent>();
        while (query.MoveNext(out var uid, out var comp))
            UpdateUi(uid, comp);
    }

    private void UpdateUi(EntityUid uid, HireTerminalComponent comp)
    {
        var leaders = GetLeaders(uid, comp);
        var mode = NeedsReview(comp, leaders) ? HireTerminalMode.NeedsReview : HireTerminalMode.SelfService;

        var status = HirePassportStatus.NoPassport;
        var holderName = string.Empty;
        var holderJob = string.Empty;
        var number = string.Empty;
        var additional = string.Empty;
        var note = string.Empty;
        var appPosition = string.Empty;

        if (TryGetPassport(uid, comp, out _, out var passport))
        {
            holderName = FullName(passport);
            holderJob  = passport.JobTitle;
            number     = passport.PassportNumber;
            additional = passport.AdditionalJob;

            if (!passport.IsBound)
            {
                status = HirePassportStatus.Invalid;
                note   = Loc.GetString("hire-terminal-note-unbound");
            }
            else if (HasAdditionalJob(passport))
            {
                status = HirePassportStatus.AlreadyHas;
            }
            else
            {
                var app = FindApplication(passport.PassportNumber);
                if (app == null)
                {
                    status = HirePassportStatus.CanApply;
                }
                else
                {
                    if (_proto.TryIndex<AdditionalPositionPrototype>(app.PositionId, out var pos))
                        appPosition = Loc.GetString(pos.Name);

                    switch (app.Status)
                    {
                        case AppStatus.Approved:
                            status = HirePassportStatus.ReadyToClaim;
                            note   = Loc.GetString("hire-terminal-note-approved-by", ("name", app.ReviewedBy));
                            break;
                        case AppStatus.Rejected:
                            status = HirePassportStatus.Rejected;
                            note   = Loc.GetString("hire-terminal-note-rejected-by", ("name", app.ReviewedBy));
                            break;
                        default:
                            if (mode == HireTerminalMode.SelfService)
                            {
                                status = HirePassportStatus.ReadyToClaim;
                                note   = Loc.GetString("hire-terminal-note-no-leader");
                            }
                            else
                            {
                                status = HirePassportStatus.Pending;
                            }
                            break;
                    }
                }
            }
        }

        var positions = new List<HirePositionInfo>();
        foreach (var pos in _proto.EnumeratePrototypes<AdditionalPositionPrototype>())
        {
            positions.Add(new HirePositionInfo(
                pos.ID,
                Loc.GetString(pos.Name),
                Loc.GetString(pos.Department),
                pos.DepartmentOrder,
                pos.Description != null ? Loc.GetString(pos.Description) : string.Empty,
                pos.Color));
        }

        positions.Sort((a, b) =>
        {
            var c = a.DepartmentOrder.CompareTo(b.DepartmentOrder);
            return c != 0 ? c : string.Compare(a.Name, b.Name, StringComparison.CurrentCulture);
        });

        var pending = new List<HirePendingInfo>();
        foreach (var app in _applications)
        {
            if (app.Status != AppStatus.Pending)
                continue;

            var posName = app.PositionId;
            var dept = string.Empty;
            if (_proto.TryIndex<AdditionalPositionPrototype>(app.PositionId, out var p))
            {
                posName = Loc.GetString(p.Name);
                dept    = Loc.GetString(p.Department);
            }

            pending.Add(new HirePendingInfo(app.Id, app.ApplicantName, app.CurrentJob, posName, dept, app.Comment, app.Time));
        }

        var log = new List<HireLogEntry>(_log);
        log.Reverse();

        var reviewers = new List<NetEntity>();
        foreach (var leader in leaders)
            reviewers.Add(GetNetEntity(leader));

        _ui.SetUiState(uid, HireTerminalUiKey.Key, new HireTerminalBoundUserInterfaceState(
            mode, status, holderName, holderJob, number, additional, note, appPosition,
            positions, pending, log, reviewers));
    }

    #endregion
}