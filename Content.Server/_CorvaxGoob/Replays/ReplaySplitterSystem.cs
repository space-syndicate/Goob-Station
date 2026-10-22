using Content.Server.GameTicking;
using Content.Shared._CorvaxGoob.CCCVars;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack; 
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Replays;
using Robust.Shared.Timing;
using System;

namespace Content.Server.Replays;

/// <summary>
/// A system for automatically splitting large server replays into parts.
/// </summary>
public sealed class ReplaySplitterSystem : EntitySystem
{
    [Dependency] private readonly IReplayRecordingManager _replayManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IResourceManager _resMan = default!;

    private long _maxCompressedBytes;
    private long _maxUncompressedBytes;

    private TimeSpan _nextCheckTime;
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(15);

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

        if (_timing.CurTime < _nextCheckTime)
            return;

        _nextCheckTime = _timing.CurTime + CheckInterval;

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

        _replayManager.StopRecording();
        _replayManager.TryStartRecording(_resMan.UserData, replayName);

        Log.Info($"[ReplaySplitter] Limit reached. New replay started: {replayName}");
    }
}
