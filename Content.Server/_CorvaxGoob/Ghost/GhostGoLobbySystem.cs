using Content.Server._CorvaxGoob.Events;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.Server.Players.PlayTimeTracking;
using Content.Server.Preferences.Managers;
using Content.Shared._CorvaxGoob.CCCVars;
using Content.Shared._CorvaxGoob.Events;
using Content.Shared.GameTicking;
using Content.Shared.Ghost;
using Content.Shared.Players.PlayTimeTracking;
using Content.Shared.Popups;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._CorvaxGoob.Ghost;

public sealed class GhostGoLobbySystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly PlayTimeTrackingManager _playTime = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IServerPreferencesManager _prefsManager = default!;

    private bool _enabled;
    private TimeSpan _requiredPlaytime;
    private TimeSpan _deathTime;

    private readonly Dictionary<NetUserId, HashSet<int>> _usedCharacterSlots = new();

    public int GetSelectedSlot(NetUserId userId)
    {
        return _prefsManager.GetPreferences(userId).SelectedCharacterIndex;
    }

    public bool CanUseCharacter(NetUserId userId, int characterSlot)
    {
        return !_usedCharacterSlots.TryGetValue(userId, out var slots) || !slots.Contains(characterSlot);
    }

    public override void Initialize()
    {
        SubscribeNetworkEvent<GhostGoLobbyEvent>(OnGhostGoLobby);
        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnRunLevelChanged);

        Subs.CVar(_cfg, CCCVars.GhostGoLobbyEnabled, value => _enabled = value, true);
        Subs.CVar(_cfg, CCCVars.GhostGoLobbyTimeHours, value => _requiredPlaytime = TimeSpan.FromHours(value), true);
        Subs.CVar(_cfg, CCCVars.GhostGoLobbyDeathTimeMinutes, value => _deathTime = TimeSpan.FromMinutes(value), true);
    }

    private void OnRunLevelChanged(GameRunLevelChangedEvent ev)
    {
        if (ev.New == GameRunLevel.PreRoundLobby)
            _usedCharacterSlots.Clear();
    }

    private void OnGhostGoLobby(GhostGoLobbyEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { Valid: true } attached)
            return;

        TryGhostGoLobby(attached, args.SenderSession);
    }

    private void TryGhostGoLobby(EntityUid uid, ICommonSession session)
    {
        if (!_enabled)
            return;

        if (_playTime.TryGetTrackerTime(session, PlayTimeTrackingShared.TrackerOverall, out var all)
            && all.Value < _requiredPlaytime)
        {
            var remaining = (int) Math.Ceiling((_requiredPlaytime - all.Value).TotalHours);
            _popup.PopupEntity(Loc.GetString("ghost-go-lobby-playtime", ("hours", remaining)), uid, uid);
            return;
        }

        if (!TryComp<GhostComponent>(uid, out var ghost))
            return;

        var timeSinceDeath = _timing.CurTime - ghost.TimeOfDeath;
        if (timeSinceDeath < _deathTime)
        {
            var remaining = (int) Math.Ceiling((_deathTime - timeSinceDeath).TotalMinutes);
            _popup.PopupEntity(Loc.GetString("ghost-go-lobby-deathtime", ("minutes", remaining)), uid, uid);
            return;
        }

        var slot = GetSelectedSlot(session.UserId);
        if (!_usedCharacterSlots.TryGetValue(session.UserId, out var slots))
            _usedCharacterSlots[session.UserId] = slots = new HashSet<int>();

        slots.Add(slot);

        _mind.WipeMind(session);

        RaiseLocalEvent(new GhostJoinLobbyRequestEvent(session));
    }
}
