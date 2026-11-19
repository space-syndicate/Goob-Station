using System.Linq;
using Content.Shared._BlackM.CCVar;
using Content.Shared._BlackM.SpeechBarks;
using Robust.Client.Audio;
using Robust.Client.Player;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Client._BlackM.SpeechBarks;

public sealed record BarkVoice(SoundSpecifier Sound, float Pitch, float Variation, float Speed, BarkMode Mode);

public sealed class SpeechBarksSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private const float BaseVolume = -6f;
    private const float WhisperVolume = -6f;
    private const float LoudVolume = 2f;
    private const float NormalHearingDistance = 10f;
    private const float WhisperHearingDistance = 5f;
    private static readonly TimeSpan PendingLifetime = TimeSpan.FromSeconds(60);

    private float _volume;
    private bool _clientEnabled;
    private bool _serverEnabled;

    private readonly Dictionary<(EntityUid Source, int MessageId), PendingVoice> _pending = new();
    private readonly List<(EntityUid Source, int MessageId)> _toRemove = new();

    private ActivePreview? _preview;

    public override void Initialize()
    {
        base.Initialize();

        _cfg.OnValueChanged(BlackMCVars.BarksVolume, OnVolumeChanged, true);
        _cfg.OnValueChanged(BlackMCVars.ReplaceTTSWithBarks, OnClientEnabledChanged, true);
        _cfg.OnValueChanged(BlackMCVars.BarksEnabled, OnServerEnabledChanged, true);

        SubscribeNetworkEvent<PlaySpeechBarksEvent>(OnPlaySpeechBarks);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _cfg.UnsubValueChanged(BlackMCVars.BarksVolume, OnVolumeChanged);
        _cfg.UnsubValueChanged(BlackMCVars.ReplaceTTSWithBarks, OnClientEnabledChanged);
        _cfg.UnsubValueChanged(BlackMCVars.BarksEnabled, OnServerEnabledChanged);

        _pending.Clear();
        _preview = null;
    }

    private void OnVolumeChanged(float value) => _volume = value;
    private void OnClientEnabledChanged(bool value) => _clientEnabled = value;
    private void OnServerEnabledChanged(bool value) => _serverEnabled = value;

    public bool Enabled => _clientEnabled && _serverEnabled;

    public bool ExpectsVoice(EntityUid uid)
    {
        return Enabled && HasComp<SpeechBarksComponent>(uid);
    }

    public BarkData? GetSpeakerData(EntityUid uid)
    {
        return TryComp<SpeechBarksComponent>(uid, out var comp) ? comp.Data : null;
    }

    private void OnPlaySpeechBarks(PlaySpeechBarksEvent ev)
    {
        if (!Enabled)
            return;

        if (!TryGetEntity(ev.Source, out var source) || Transform(source.Value).MapID == MapId.Nullspace)
            return;

        var voice = new BarkVoice(ev.Sound, ev.Pitch, ev.Variation, ev.Speed, ev.Mode);
        _pending[(source.Value, ev.MessageId)] = new PendingVoice(voice, _timing.RealTime + PendingLifetime);
    }

    public bool TryClaimVoice(EntityUid uid, int messageId, out BarkVoice voice)
    {
        voice = default!;
        if (!_pending.Remove((uid, messageId), out var pending) || pending.Expires < _timing.RealTime)
            return false;

        voice = pending.Voice;
        return true;
    }

    public void PlayCue(EntityUid? source, BarkVoice voice, BarkCue cue, bool whisper, bool loud)
    {
        if (_volume <= 0f || _player.LocalSession is not { } session)
            return;

        var volume = BaseVolume + SharedAudioSystem.GainToVolume(_volume);
        if (whisper)
            volume += WhisperVolume;
        if (loud)
            volume += LoudVolume;

        var variation = voice.Variation > 0f ? _random.NextFloat(-voice.Variation, voice.Variation) : 0f;
        var pitch = Math.Clamp(voice.Pitch * (1f + cue.PitchShift) * (1f + variation), 0.2f, 4f);

        var audioParams = AudioParams.Default
            .WithPitchScale(pitch)
            .WithVolume(volume)
            .WithMaxDistance(whisper ? WhisperHearingDistance : NormalHearingDistance);

        var sound = _audio.ResolveSound(voice.Sound);

        if (source == null)
        {
            _audio.PlayGlobal(sound, session, audioParams);
            return;
        }

        if (Deleted(source.Value))
            return;

        if (source == _player.LocalEntity)
            _audio.PlayGlobal(sound, source.Value, audioParams);
        else
            _audio.PlayEntity(sound, session, source.Value, audioParams);
    }

    public bool TryResolveSound(BarkData data, out SoundSpecifier sound)
    {
        if (data.Sound != null)
        {
            sound = data.Sound;
            return true;
        }

        if (_proto.TryIndex(data.Proto, out var proto) ||
            _proto.TryIndex<BlackMBarkPrototype>(BarkData.DefaultProto, out proto))
        {
            sound = proto.Sound;
            return true;
        }

        sound = default!;
        return false;
    }

    public void PlayPreview(BarkData data, string text, Action<int>? onReveal = null, Action? onFinished = null)
    {
        StopPreview();

        if (string.IsNullOrWhiteSpace(text) || !TryResolveSound(data, out var sound))
            return;

        var voice = new BarkVoice(sound, data.Pitch, data.Variation, data.Speed, data.Mode);
        var timeline = SpeechBarkTimeline.Build(text, data.Speed, data.Mode);

        _preview = new ActivePreview(voice, timeline, _timing.RealTime, text.TrimEnd().EndsWith('!'), onReveal, onFinished);
    }

    public void StopPreview()
    {
        var preview = _preview;
        _preview = null;
        preview?.OnFinished?.Invoke();
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        UpdatePreview();

        if (_pending.Count == 0)
            return;

        var now = _timing.RealTime;
        foreach (var (key, pending) in _pending)
        {
            if (pending.Expires < now || Deleted(key.Source))
                _toRemove.Add(key);
        }

        foreach (var key in _toRemove)
        {
            _pending.Remove(key);
        }

        _toRemove.Clear();
    }

    private void UpdatePreview()
    {
        if (_preview is not { } preview)
            return;

        var elapsed = (float) (_timing.RealTime - preview.Start).TotalSeconds;
        var timeline = preview.Timeline;

        var revealed = timeline.RevealedAt(elapsed);
        if (revealed != preview.Revealed)
        {
            preview.Revealed = revealed;
            preview.OnReveal?.Invoke(revealed);
        }

        var due = -1;
        while (preview.NextCue < timeline.Cues.Count && timeline.Cues[preview.NextCue].Time <= elapsed)
        {
            due = preview.NextCue++;
        }

        if (due >= 0)
            PlayCue(null, preview.Voice, timeline.Cues[due], false, preview.Loud);

        if (elapsed >= timeline.Duration && preview.NextCue >= timeline.Cues.Count && _preview == preview)
        {
            _preview = null;
            preview.OnReveal?.Invoke(timeline.Length);
            preview.OnFinished?.Invoke();
        }
    }

    public static List<BlackMBarkPrototype> GetSelectableBarks(IPrototypeManager proto)
    {
        return proto.EnumeratePrototypes<BlackMBarkPrototype>()
            .Where(b => b.RoundStart)
            .OrderBy(b => Robust.Shared.Localization.Loc.GetString(b.Name).ToLowerInvariant())
            .ToList();
    }

    private readonly record struct PendingVoice(BarkVoice Voice, TimeSpan Expires);

    private sealed class ActivePreview
    {
        public readonly BarkVoice Voice;
        public readonly SpeechBarkTimeline Timeline;
        public readonly TimeSpan Start;
        public readonly bool Loud;
        public readonly Action<int>? OnReveal;
        public readonly Action? OnFinished;

        public int NextCue;
        public int Revealed = -1;

        public ActivePreview(BarkVoice voice, SpeechBarkTimeline timeline, TimeSpan start, bool loud, Action<int>? onReveal, Action? onFinished)
        {
            Voice = voice;
            Timeline = timeline;
            Start = start;
            Loud = loud;
            OnReveal = onReveal;
            OnFinished = onFinished;
        }
    }
}
