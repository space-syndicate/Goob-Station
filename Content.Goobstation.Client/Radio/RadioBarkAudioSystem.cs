using Content.Client.Audio;
using Content.Goobstation.Common.CCVar;
using Content.Goobstation.Shared.Radio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Goobstation.Client.Radio;

public sealed class RadioBarkAudioSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    private float _volume = 1f;
    private float _cooldown = 1f;
    private TimeSpan _nextRadioSound;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<PlayRadioBarkEvent>(OnPlayRadioBark);
        Subs.CVar(_cfg, GoobCVars.RadioVolume, value => _volume = value, true);
        Subs.CVar(_cfg, GoobCVars.RadioSoundCooldown, value => _cooldown = MathF.Max(0f, value), true);
    }

    private void OnPlayRadioBark(PlayRadioBarkEvent ev)
    {
        if (_volume <= 0f || _timing.CurTime < _nextRadioSound)
            return;

        _nextRadioSound = _timing.CurTime + TimeSpan.FromSeconds(_cooldown);

        var audioParams = ev.Sound.Params.AddVolume(SharedAudioSystem.GainToVolume(_volume));
        _audio.PlayGlobal(_audio.ResolveSound(ev.Sound), Filter.Local(), false, audioParams);
    }
}
