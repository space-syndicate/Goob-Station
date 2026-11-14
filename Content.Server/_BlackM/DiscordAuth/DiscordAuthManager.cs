using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Content.Corvax.Interfaces.Server;
using Content.Shared._BlackM.CCVar;
using Content.Shared._BlackM.DiscordAuth;
using Robust.Server.Player;
using Robust.Server.ServerStatus;
using Robust.Shared.Asynchronous;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server._BlackM.DiscordAuth;

public sealed class DiscordAuthManager : IServerDiscordAuthManager
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IServerNetManager _netManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IStatusHost _statusHost = default!;
    [Dependency] private readonly IResourceManager _resourceManager = default!;
    [Dependency] private readonly ITaskManager _taskManager = default!;

    public event EventHandler<ICommonSession>? PlayerVerified;

    private sealed class PendingLink
    {
        public NetUserId UserId;
        public string Ckey = string.Empty;
        public string Code = string.Empty;
        public DateTime Expires;
    }

    private const string LinkPath = "/discord/link";
    private const string CodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int CodeLength = 8;
    private const int MaxBodyBytes = 2048;
    private const int MaxBadCodesPerMinute = 20;

    private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan CodeRefreshThreshold = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan SignatureWindow = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan CheckCooldown = TimeSpan.FromSeconds(2);

    private ISawmill _sawmill = default!;
    private DiscordAuthStorage _storage = default!;

    private readonly object _lock = new();
    private readonly Dictionary<string, PendingLink> _byCode = new();
    private readonly Dictionary<NetUserId, PendingLink> _byUser = new();
    private readonly Queue<DateTime> _badCodes = new();

    // главный поток.
    private readonly Dictionary<NetUserId, DateTime> _lastCheck = new();

    private bool _enabled;
    private bool _isOptional;

    private readonly HashSet<NetUserId> _bypassedUsers = new();
    private string _secret = string.Empty;
    private HashSet<IPAddress> _allowedIps = new();

    public void Initialize()
    {
        _sawmill = Logger.GetSawmill("blackm.discord_auth");
        _storage = new DiscordAuthStorage(
            Path.Combine(_resourceManager.UserData.RootDir?.ToString() ?? "data", "discord_auth"),
            _sawmill);

        _netManager.RegisterNetMessage<MsgDiscordAuthCheck>(OnAuthCheck);
        _netManager.RegisterNetMessage<MsgDiscordAuthByPass>(OnAuthByPass);
        _netManager.RegisterNetMessage<MsgDiscordAuthRequired>();
        _netManager.RegisterNetMessage<MsgDiscordAuthVerified>();

        _cfg.OnValueChanged(BlackMCVars.DiscordAuthEnabled, v => _enabled = v, true);
        _cfg.OnValueChanged(BlackMCVars.DiscordAuthIsOptional, v => _isOptional = v, true);
        _cfg.OnValueChanged(BlackMCVars.DiscordAuthLinkSecret, v => _secret = v, true);
        _cfg.OnValueChanged(BlackMCVars.DiscordAuthLinkAllowedIps, OnAllowedIpsChanged, true);

        if (_enabled && string.IsNullOrEmpty(_secret))
            _sawmill.Warning("discord_auth.link_secret is empty: /discord/link is disabled, nobody can verify!");

        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
        _statusHost.AddHandler(OnHttpRequest);
    }

    private void OnAllowedIpsChanged(string value)
    {
        var set = new HashSet<IPAddress>();
        foreach (var part in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (IPAddress.TryParse(part, out var ip))
                set.Add(ip.IsIPv4MappedToIPv6 ? ip.MapToIPv4() : ip);
            else
                _sawmill.Warning($"discord_auth.link_allowed_ips: '{part}' is not a valid IP, ignored");
        }

        _allowedIps = set;
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (e.NewStatus == SessionStatus.Disconnected)
        {
            _bypassedUsers.Remove(e.Session.UserId);
            _lastCheck.Remove(e.Session.UserId);
            DropPending(e.Session.UserId);
            return;
        }

        if (!_enabled || e.NewStatus != SessionStatus.Connected)
            return;

        SendAuthRequiredIfNeeded(e.Session);
    }

    private void OnAuthCheck(MsgDiscordAuthCheck message)
    {
        if (!_playerManager.TryGetSessionByChannel(message.MsgChannel, out var session))
            return;

        var now = DateTime.UtcNow;
        if (_lastCheck.TryGetValue(session.UserId, out var last) && now - last < CheckCooldown)
            return;

        _lastCheck[session.UserId] = now;
        SendAuthRequiredIfNeeded(session);
    }

    private void OnAuthByPass(MsgDiscordAuthByPass message)
    {
        if (!_isOptional)
            return;

        if (!_playerManager.TryGetSessionByChannel(message.MsgChannel, out var session))
            return;

        _bypassedUsers.Add(session.UserId);
        _netManager.ServerSendMessage(new MsgDiscordAuthVerified(), message.MsgChannel);
        PlayerVerified?.Invoke(this, session);
    }

    public bool CanJoinRound(NetUserId userId)
    {
        if (!_enabled)
            return true;

        if (_isOptional && _bypassedUsers.Contains(userId))
            return true;

        try
        {
            return IsVerifiedInternal(userId);
        }
        catch (Exception ex)
        {
            _sawmill.Error($"Failed to check discord verification for {userId}: {ex}");
            return false;
        }
    }

    public void NotifyJoinBlocked(ICommonSession session)
    {
        try
        {
            var pending = GetOrCreatePending(session);
            _netManager.ServerSendMessage(new MsgDiscordAuthRequired
            {
                LinkCode = FormatLinkCode(pending.Code),
                ShowWindow = true,
            }, session.Channel);
        }
        catch (Exception ex)
        {
            _sawmill.Error($"Failed to send discord join block to {session.UserId}: {ex}");
        }
    }

    private void SendAuthRequiredIfNeeded(ICommonSession session)
    {
        try
        {
            if (IsVerifiedInternal(session.UserId))
            {
                _netManager.ServerSendMessage(new MsgDiscordAuthVerified(), session.Channel);
                return;
            }

            var pending = GetOrCreatePending(session);
            _netManager.ServerSendMessage(new MsgDiscordAuthRequired
            {
                LinkCode = FormatLinkCode(pending.Code),
            }, session.Channel);
        }
        catch (Exception ex)
        {
            _sawmill.Error($"Failed to send discord auth requirement to {session.UserId}: {ex}");
        }
    }

    public Task<bool> IsVerified(NetUserId userId, CancellationToken cancel)
        => Task.FromResult(IsVerifiedInternal(userId));

    private bool IsVerifiedInternal(NetUserId userId)
        => !_enabled || _storage.IsVerified(userId.UserId.ToString());

    private PendingLink GetOrCreatePending(ICommonSession session)
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;
            PurgeExpired(now);

            if (_byUser.TryGetValue(session.UserId, out var existing))
            {
                if (existing.Expires - now > CodeRefreshThreshold)
                    return existing;

                _byCode.Remove(existing.Code);
                _byUser.Remove(session.UserId);
            }

            var created = new PendingLink
            {
                UserId = session.UserId,
                Ckey = session.Name,
                Code = GenerateCode(),
                Expires = now + CodeLifetime,
            };

            _byCode[created.Code] = created;
            _byUser[created.UserId] = created;
            return created;
        }
    }

    private void DropPending(NetUserId userId)
    {
        lock (_lock)
        {
            if (_byUser.Remove(userId, out var p))
                _byCode.Remove(p.Code);
        }
    }

    // _lock
    private void PurgeExpired(DateTime now)
    {
        List<PendingLink>? dead = null;
        foreach (var p in _byCode.Values)
        {
            if (p.Expires <= now)
                (dead ??= new()).Add(p);
        }

        if (dead == null)
            return;

        foreach (var p in dead)
        {
            _byCode.Remove(p.Code);
            _byUser.Remove(p.UserId);
        }
    }

    // _lock.
    private string GenerateCode()
    {
        while (true)
        {
            Span<char> chars = stackalloc char[CodeLength];
            for (var i = 0; i < chars.Length; i++)
                chars[i] = CodeAlphabet[RandomNumberGenerator.GetInt32(CodeAlphabet.Length)];

            var code = new string(chars);
            if (!_byCode.ContainsKey(code))
                return code;
        }
    }

    private static string FormatLinkCode(string code)
        => code.Length == CodeLength ? $"{code[..4]}-{code[4..]}" : code;

    private async Task<bool> OnHttpRequest(IStatusHandlerContext context)
    {
        if (context.RequestMethod != HttpMethod.Post || context.Url.AbsolutePath != LinkPath)
            return false;

        if (!_enabled || string.IsNullOrEmpty(_secret))
            return false;

        if (_allowedIps.Count > 0)
        {
            var ip = context.RemoteEndPoint.Address;
            if (ip.IsIPv4MappedToIPv6)
                ip = ip.MapToIPv4();

            if (!_allowedIps.Contains(ip))
                return false;
        }

        try
        {
            await HandleLink(context);
        }
        catch (Exception ex)
        {
            _sawmill.Error($"Discord link request failed: {ex}");
            await Error(context, HttpStatusCode.InternalServerError, "internal");
        }

        return true;
    }

    private async Task HandleLink(IStatusHandlerContext context)
    {
        var body = await ReadBodyAsync(context.RequestBody);
        if (body == null)
        {
            await Error(context, HttpStatusCode.RequestEntityTooLarge, "too_large");
            return;
        }

        var timestamp = GetHeader(context, "X-Timestamp");
        var signature = GetHeader(context, "X-Signature");

        if (!long.TryParse(timestamp, out var ts)
            || Math.Abs(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - ts) > SignatureWindow.TotalSeconds
            || !VerifySignature(timestamp, body, signature))
        {
            await Error(context, HttpStatusCode.Unauthorized, "unauthorized");
            return;
        }

        if (IsRateLimited())
        {
            await Error(context, HttpStatusCode.TooManyRequests, "rate_limited");
            return;
        }

        DiscordLinkRequest? request;
        try
        {
            request = JsonSerializer.Deserialize<DiscordLinkRequest>(body);
        }
        catch (JsonException)
        {
            request = null;
        }

        var code = (request?.Code ?? string.Empty)
            .Replace("-", "").Replace(" ", "").Trim().ToUpperInvariant();

        if (request == null
            || code.Length != CodeLength
            || request.DiscordId.Length is 0 or > 20
            || !ulong.TryParse(request.DiscordId, out _))
        {
            await Error(context, HttpStatusCode.BadRequest, "bad_request");
            return;
        }

        PendingLink? entry;
        lock (_lock)
        {
            PurgeExpired(DateTime.UtcNow);
            if (_byCode.Remove(code, out entry))
                _byUser.Remove(entry.UserId);
        }

        if (entry == null)
        {
            RegisterBadCode();
            await Error(context, HttpStatusCode.NotFound, "code_not_found");
            return;
        }

        if (!_storage.TryLink(entry.UserId.UserId.ToString(), entry.Ckey, request.DiscordId, out _))
        {
            lock (_lock)
            {
                if (entry.Expires > DateTime.UtcNow && !_byUser.ContainsKey(entry.UserId))
                {
                    _byCode[entry.Code] = entry;
                    _byUser[entry.UserId] = entry;
                }
            }

            await Error(context, HttpStatusCode.Conflict, "discord_already_linked");
            return;
        }

        NotifyVerified(entry.UserId);

        _sawmill.Info($"Linked discord {request.DiscordId} to {entry.Ckey} ({entry.UserId})");
        await context.RespondAsync(
            JsonSerializer.Serialize(new { ok = true, ckey = entry.Ckey, userId = entry.UserId.UserId.ToString() }),
            HttpStatusCode.OK,
            "application/json");
    }

    private void NotifyVerified(NetUserId userId)
    {
        _taskManager.RunOnMainThread(() =>
        {
            if (!_playerManager.TryGetSessionById(userId, out var session))
                return;

            _netManager.ServerSendMessage(new MsgDiscordAuthVerified(), session.Channel);
            PlayerVerified?.Invoke(this, session);
        });
    }

    private bool VerifySignature(string timestamp, byte[] body, string signatureHex)
    {
        byte[] provided;
        try
        {
            provided = Convert.FromHexString(signatureHex);
        }
        catch (FormatException)
        {
            return false;
        }

        var prefix = Encoding.UTF8.GetBytes(timestamp + ".");
        var message = new byte[prefix.Length + body.Length];
        Buffer.BlockCopy(prefix, 0, message, 0, prefix.Length);
        Buffer.BlockCopy(body, 0, message, prefix.Length, body.Length);

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secret));
        return CryptographicOperations.FixedTimeEquals(provided, hmac.ComputeHash(message));
    }

    private bool IsRateLimited()
    {
        lock (_lock)
        {
            var cutoff = DateTime.UtcNow.AddMinutes(-1);
            while (_badCodes.Count > 0 && _badCodes.Peek() < cutoff)
                _badCodes.Dequeue();

            return _badCodes.Count >= MaxBadCodesPerMinute;
        }
    }

    private void RegisterBadCode()
    {
        lock (_lock)
            _badCodes.Enqueue(DateTime.UtcNow);
    }

    private static string GetHeader(IStatusHandlerContext context, string name)
    {
        foreach (var (key, value) in context.RequestHeaders)
        {
            if (key.Equals(name, StringComparison.OrdinalIgnoreCase))
                return value.ToString();
        }

        return string.Empty;
    }

    private static async Task<byte[]?> ReadBodyAsync(Stream stream)
    {
        using var ms = new MemoryStream();
        var buffer = new byte[512];
        int read;
        while ((read = await stream.ReadAsync(buffer)) > 0)
        {
            ms.Write(buffer, 0, read);
            if (ms.Length > MaxBodyBytes)
                return null;
        }

        return ms.ToArray();
    }

    private static Task Error(IStatusHandlerContext context, HttpStatusCode status, string error)
        => context.RespondAsync($"{{\"error\":\"{error}\"}}", status, "application/json");
}