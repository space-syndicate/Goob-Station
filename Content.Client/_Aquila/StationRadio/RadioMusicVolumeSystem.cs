using Content.Goobstation.Shared.StationRadio.Components;
using Content.Shared._Aquila.CCVars;
using Robust.Client.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;

namespace Content.Client._Aquila.StationRadio;

public sealed class RadioMusicVolumeSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private EntityQuery<VinylPlayerComponent> _playerQuery;
    private EntityQuery<StationRadioReceiverComponent> _receiverQuery;
    private float _gain = 1f;
    private float _volumeOffset;

    public override void Initialize()
    {
        base.Initialize();

        UpdatesAfter.Add(typeof(AudioSystem));

        _playerQuery = GetEntityQuery<VinylPlayerComponent>();
        _receiverQuery = GetEntityQuery<StationRadioReceiverComponent>();

        Subs.CVar(_cfg, AquilaCVars.RadioMusicVolume, OnVolumeChanged, true);
    }

    private void OnVolumeChanged(float value)
    {
        _gain = value;
        _volumeOffset = SharedAudioSystem.GainToVolume(value);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (MathHelper.CloseTo(_gain, 1f))
            return;

        var query = EntityQueryEnumerator<AudioComponent, TransformComponent>();
        while (query.MoveNext(out var audio, out var xform))
        {
            if (audio.Gain <= 0f)
                continue;

            var parent = xform.ParentUid;
            if (!_playerQuery.HasComp(parent) && !_receiverQuery.HasComp(parent))
                continue;

            audio.Volume = audio.Params.Volume + _volumeOffset;
        }
    }
}
