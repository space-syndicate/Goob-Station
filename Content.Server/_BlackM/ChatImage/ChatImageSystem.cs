using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Shared._BlackM.ChatImage;
using Content.Shared.Chat;
using Content.Shared.Database;
using Robust.Shared.Asynchronous;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Localization;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server._BlackM.ChatImage;

public sealed class ChatImageSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly ITaskManager _task = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;

    private int _nextId = 1;

    public void SendFromUrl(IConsoleShell shell, string adminName, NetUserId? adminUserId, string rawUrl, string caption)
    {
        if (!_cfg.GetCVar(BlackMCVars.ChatImageEnabled))
        {
            shell.WriteError(Loc.GetString("cmd-sendimg-error-disabled"));
            return;
        }

        if (!Uri.TryCreate(rawUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
        {
            shell.WriteError(Loc.GetString("cmd-sendimg-error-bad-url"));
            return;
        }

        var hosts = _cfg.GetCVar(BlackMCVars.ChatImageHosts)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (!hosts.Any(h => string.Equals(h, uri.Host, StringComparison.OrdinalIgnoreCase)))
        {
            shell.WriteError(Loc.GetString("cmd-sendimg-error-host-not-allowed", ("host", uri.Host)));
            return;
        }

        var maxBytes = _cfg.GetCVar(BlackMCVars.ChatImageMaxKb) * 1024;

        shell.WriteLine(Loc.GetString("cmd-sendimg-loading"));

        Task.Run(async () =>
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                var result = await ChatImageProcessor.LoadAsync(uri, maxBytes, cts.Token);

                _task.RunOnMainThread(() => Publish(shell, adminName, adminUserId, uri, caption, result));
            }
            catch (ChatImageException e)
            {
                _task.RunOnMainThread(() => shell.WriteError(Loc.GetString(e.LocKey, e.Args)));
            }
            catch (Exception e)
            {
                Log.Warning($"sendimg failed for {uri}: {e}");
                _task.RunOnMainThread(() => shell.WriteError(Loc.GetString("cmd-sendimg-error-failed")));
            }
        });
    }

    private void Publish(IConsoleShell shell, string adminName, NetUserId? adminUserId, Uri uri, string caption, ChatImageProcessor.Result result)
    {
        var id = _nextId++;

        RaiseNetworkEvent(new ChatImageDataEvent(id, result.Frames, result.DelaysMs), Filter.Broadcast());

        var message = string.IsNullOrWhiteSpace(caption)
            ? string.Empty
            : FormattedMessage.EscapeText(caption) + "\n";

        var scale = Math.Min(ChatImageConstants.MaxUpscale, Math.Min(
            ChatImageConstants.MaxDisplayWidth / result.Width,
            ChatImageConstants.MaxDisplayHeight / result.Height));
        var displayHeight = result.Height * scale;
        var lines = (int) Math.Ceiling(displayHeight / ChatImageConstants.LineHeightEstimate);

        var spacer = new string('\n', lines) + "\u200B";

        var wrapped = Loc.GetString("chat-manager-send-ooc-wrap-message",
            ("playerName", FormattedMessage.EscapeText(adminName)),
            ("message", $"{message}\n[img={id}][/img]{spacer}"));

        _chat.ChatMessageToAll(ChatChannel.OOC, caption, wrapped, EntityUid.Invalid, false, false, author: adminUserId);

        _adminLog.Add(LogType.AdminMessage, LogImpact.Low,
            $"{adminName} sent chat image {uri} (frames: {result.Frames.Count}, caption: {caption})");

        shell.WriteLine(Loc.GetString("cmd-sendimg-sent", ("frames", result.Frames.Count)));
    }
}
