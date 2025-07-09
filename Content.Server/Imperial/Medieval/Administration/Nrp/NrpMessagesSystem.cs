using System.Linq;
using System.Text.RegularExpressions;
using Content.Server.Administration.Managers;
using Content.Server.Chat.Systems;
using Content.Server.Database;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Filter = Robust.Shared.Player.Filter;
using Robust.Shared.Prototypes;
using Content.Server.GameTicking;
using Content.Shared.Imperial.Medieval.Admin;
using Robust.Server.Player;
using Content.Shared.Imperial.Medieval.Administration.Nrp;
using Robust.Shared.Network;
using System.Threading.Tasks;
using Content.Shared.Administration;
using Content.Shared.Database;
using Robust.Shared.Player;

namespace Content.Server.Imperial.Medieval.Administration.Nrp;

public sealed partial class NrpMessagesSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IBanManager _banManager = default!;



    private readonly Dictionary<string, bool> _bannedWords = new() { };

    private const string WordBoundaryPattern = @"\b[\p{L}\p{N}_]+\b";
    private readonly Regex _compiledPattern = new(WordBoundaryPattern, RegexOptions.Compiled);

    private readonly List<NrpMessage> _unsolvedMessages = new();
    private readonly List<NrpPanelEui> _activeEuis = new();
    private readonly Dictionary<string, int> _stats = new();

    public void AddResolveToStats(string administrator)
    {
        _stats.TryAdd(administrator, 0);
        _stats[administrator]++;
    }

    public Dictionary<string, int> GetStats() => _stats;

    public void RegisterEui(NrpPanelEui eui)
    {
        _activeEuis.Add(eui);
    }

    public void UnregisterEui(NrpPanelEui eui)
    {
        _activeEuis.Remove(eui);
    }

    public void RemoveMessage(NrpMessage message)
    {
        _unsolvedMessages.Remove(message);
        foreach (var eui in _activeEuis)
            eui.SendRemoveMessage(message);
    }

    private void Bwoink(ICommonSession player, NetUserId sender, string message)
    {
        var bwoinkMessage = new SharedBwoinkSystem.BwoinkTextMessage(
            player.UserId,
            sender,
            Loc.GetString("nrp-panel-ahelp-message", ("message", message)));

        RaiseNetworkEvent(bwoinkMessage, player.Channel);
    }

    private void Ban(NetUserId playerId, string playerName, NetUserId sender, string message, uint banMinutes)
    {
        _banManager.CreateServerBan(playerId,
            playerName,
            sender,
            null, // думаю бан по ip и hwid здесь неуместен
            null,
            banMinutes,
            NoteSeverity.Minor,
            Loc.GetString(Loc.GetString("nrp-panel-ban-message", ("message", message))));
    }

    public void OnViolation(NrpMessage message, int violationCount, NetUserId sender)
    {
        var playerId = message.PlayerId;
        var playerName = message.PlayerName;

        if (violationCount == 1)
        {
            if (_playerManager.TryGetSessionById(playerId, out var session))
                Bwoink(session, sender, message.Message);
        }
        else
        {
            var banHours = (uint)(Math.Pow(2, 2 * (violationCount - 2)));
            var banMinutes = banHours * 60;
            Ban(playerId, playerName, sender, message.Message, banMinutes);
        }
    }



    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartAttemptEvent>(OnMapInit);
        SubscribeLocalEvent<EntitySpokeEvent>(CheckMessage);
    }

    public List<NrpMessage> GetAllMessages()
    {
        return _unsolvedMessages;
    }

    public async Task<int> GetPlayerNrpViolations(NetUserId player, int daysCount)
    {
        return await _db.GetLastNrpViolationsCount(player, daysCount);
    }

    public async Task AddPlayerNrpViolation(NetUserId player)
    {
        await _db.AddNrpViolation(player);
    }

    private void OnMapInit(RoundStartAttemptEvent ev)
    {
        if (ev.Cancelled)
            return;

        var notifPrototypes = _prototype.EnumeratePrototypes<MessageNotifPrototype>().ToList();
        foreach (var i in notifPrototypes)
        {
            foreach (var x in i.Notif)
            {
                _bannedWords.TryAdd(x, i.IsSubword);
            }
        }
    }

    private IEnumerable<string> TokenizeText(string input)
    {
        if (string.IsNullOrEmpty(input))
            return [];

        return _compiledPattern
            .Matches(input)
            .Cast<Match>()
            .Select(match => match.Value.ToLowerInvariant());
    }

    public Dictionary<string, bool> GetBannedWords(string input, Dictionary<string, bool> bannedWords)
    {
        var matches = new Dictionary<string, bool>();

        foreach (var (word, isSubword) in bannedWords)
        {
            if (isSubword)
            {
                if (input.Contains(word, StringComparison.OrdinalIgnoreCase))
                    matches.TryAdd(word, true);
            }
            else
            {
                var tokens = TokenizeText(input);

                if (tokens.Contains(word))
                    matches.TryAdd(word, false);
            }
        }

        return matches;
    }

    private string WrapBannedWordsInTag(string input, Dictionary<string, bool> bannedWords)
    {
        if (string.IsNullOrEmpty(input) || bannedWords.Count == 0)
            return input;

        var result = input;

        var subwords = bannedWords
            .Where(w => w.Value)
            .Select(w => w.Key);

        var fullWords = bannedWords
            .Where(w => !w.Value)
            .Select(w => w.Key);

        var patterns = new List<string>();

        patterns.AddRange(subwords.Select(Regex.Escape));
        patterns.AddRange(fullWords.Select(w => $"\\b{Regex.Escape(w)}\\b"));


        if (patterns.Any())
        {
            var combinedPattern = string.Join("|", patterns);
            result = Regex.Replace(
                result,
                combinedPattern,
                match => $"[color=red]{match.Value}[/color]",
                RegexOptions.IgnoreCase);
        }

        return result;
    }

    private void CheckMessage(EntitySpokeEvent ev)
    {
        if (_bannedWords.Count == 0)
            return;

        if (!_playerManager.TryGetSessionByEntity(ev.Source, out var session))
            return;

        var message = ev.Message;
        var matches = GetBannedWords(message, _bannedWords);
        if (matches.Count == 0)
            return;

        var formattedMessage = WrapBannedWordsInTag(message, matches);

        var senderNetEntity = GetNetEntity(session.AttachedEntity);

        var nrpMessage = new NrpMessage(formattedMessage, session.Name, session.UserId, senderNetEntity);
        _unsolvedMessages.Add(nrpMessage);

        foreach (var eui in _activeEuis)
        {
            eui.SendNewMessage(nrpMessage);
        }


        _audio.PlayGlobal("/Audio/Imperial/Medieval/Misk/pop.ogg", Filter.Empty().AddPlayers(_adminManager.ActiveAdmins), true, AudioParams.Default.WithVolume(-4f));
    }
}
