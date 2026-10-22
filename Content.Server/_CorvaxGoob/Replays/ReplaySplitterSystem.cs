using Content.Server.GameTicking;
using Content.Shared._CorvaxGoob.CCCVars;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Replays;
using System;

namespace Content.Server.Replays;

/// <summary>
/// A system for automatically splitting large server replays into parts.
/// </summary>
public sealed class ReplaySplitterSystem : EntitySystem
{
    [Dependency] private readonly IConsoleHost _console = default!;
    [Dependency] private readonly IReplayRecordingManager _replayManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private long _maxCompressedBytes;
    private long _maxUncompressedBytes;

    private float _timer;
    private const float CheckIntervalSeconds = 15f;

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_cfg, CCCVars.SplitCompressedSize, v => _maxCompressedBytes = v * 1024L, true);
        Subs.CVar(_cfg, CCCVars.SplitUncompressedSize, v => _maxUncompressedBytes = v * 1024L, true);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_replayManager.IsRecording)
            return;

        _timer += frameTime;
        if (_timer < CheckIntervalSeconds)
            return;

        _timer = 0f;

        var stats = _replayManager.GetReplayStats();

        if (stats.Size >= _maxCompressedBytes || stats.UncompressedSize >= _maxUncompressedBytes)
        {
            SplitReplay();
        }
    }

    private void SplitReplay()
    {
        var ticker = Get<GameTicker>();

        var dateStr = DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm");
        var timeStr = ticker.RoundDuration().ToString(@"hh\-mm\-ss");

        var replayName = $"{dateStr}-round_{ticker.RoundId}-started_{timeStr}";

        _console.ExecuteCommand("replay_recording_stop");
        _console.ExecuteCommand($"replay_recording_start \"{replayName}\"");

        _console.ExecuteCommand($"echo [ReplaySplitter] Limit reached. New replay started: {replayName}");
    }
}
