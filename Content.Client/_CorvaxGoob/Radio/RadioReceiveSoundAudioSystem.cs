// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._CorvaxGoob.CCCVars;
using Content.Shared._CorvaxGoob.Radio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Client._CorvaxGoob.Radio;

/// <summary>
/// Plays local receive sounds for headset radio messages.
/// </summary>
public sealed partial class RadioReceiveSoundAudioSystem : EntitySystem
{
    // Keep repeated radio receive sounds varied without needing sound collections.
    private const float SoundParameterVariation = 0.3f;

    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    private float _volume = 1f;
    private float _cooldown = 1f;
    private TimeSpan _nextRadioSound;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<PlayRadioReceiveSoundEvent>(OnPlayRadioReceiveSound);
        Subs.CVar(_cfg, CCCVars.RadioVolume, value => _volume = value, true);
        Subs.CVar(_cfg, CCCVars.RadioSoundCooldown, value => _cooldown = Math.Clamp(value, 1f, 10f), true);
    }

    private void OnPlayRadioReceiveSound(PlayRadioReceiveSoundEvent ev)
    {
        if (_volume <= 0f || _timing.CurTime < _nextRadioSound)
            return;

        _nextRadioSound = _timing.CurTime + TimeSpan.FromSeconds(_cooldown);

        var pitchScale = GetRandomParameterScale();
        var volumeScale = GetRandomParameterScale();

        var audioParams = ev.Sound.Params
            .WithPitchScale(ev.Sound.Params.Pitch * pitchScale)
            .AddVolume(SharedAudioSystem.GainToVolume(_volume))
            .AddVolume(SharedAudioSystem.GainToVolume(volumeScale));

        _audio.PlayGlobal(_audio.ResolveSound(ev.Sound), Filter.Local(), false, audioParams);
    }

    private float GetRandomParameterScale()
    {
        return _random.NextFloat(1f - SoundParameterVariation, 1f + SoundParameterVariation);
    }
}
