using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;
using Robust.Shared.Configuration;
using Content.Server.Radio.EntitySystems;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.GameTicking;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Server.RoundEnd;
using Content.Server.Audio;
using Content.Server.Explosion.EntitySystems;
using Content.Server.AlertLevel;
using Content.Server.Station.Systems;
using Content.Shared.Audio;
using Content.Shared.Imperial.ICCVar;
using Content.Shared.Imperial.EnergyCore;
using Content.Server.Imperial.EnergyCore.Components;

namespace Content.Server.Imperial.EnergyCore;

public sealed class EnergyCorePendingDetonationSystem : EntitySystem
{
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly AlertLevelSystem _alertLevel = default!;
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedAmbientSoundSystem _ambientSound = default!;
    [Dependency] private readonly ServerGlobalSoundSystem _sound = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EnergyCorePendingDetonationComponent, ComponentStartup>(OnComponentStartup);
    }

    private void OnComponentStartup(EntityUid uid, EnergyCorePendingDetonationComponent component, ComponentStartup args)
    {
        // Определение времени до детонации через цварку
        var detonationTime = _cfg.GetCVar(ICCVars.CoreDetonationTime);
        component.DetonationTime = TimeSpan.FromSeconds(detonationTime);

        // Ставим новое имя и описание сущности
        _metaData.SetEntityName(uid, Loc.GetString("energycore-meltdown-name"));
        _metaData.SetEntityDescription(uid, Loc.GetString("energycore-meltdown-desc"));

        var ev = new CoreCompromisedEvent();
        RaiseLocalEvent(ev);

        GetDelayTime(uid, component);
        AnnounceCatastroph(uid, component);
        PlayBackgroundSiren(uid, component);
    }
    private void GetDelayTime(EntityUid uid, EnergyCorePendingDetonationComponent component)
    {
        // К полученному от цварки времени добавляем текущее время
        component.DetonationTime = _timing.CurTime + component.DetonationTime;
        GetMusicTime(uid, component);
    }
    private void GetMusicTime(EntityUid uid, EnergyCorePendingDetonationComponent component)
    {
        var sound = _audio.ResolveSound(component.MeltdownMusic);
        var audioLength = _audio.GetAudioLength(sound).TotalSeconds;
        component.MusicTime = component.DetonationTime - TimeSpan.FromSeconds(audioLength);
    }
    private void AnnounceCatastroph(EntityUid uid, EnergyCorePendingDetonationComponent component)
    {
        var station = _stationSystem.GetOwningStation(uid);
        if (station != null)
        {
            var alertLevel = _cfg.GetCVar(ICCVars.AlertLevelOnMeltdown);
            _alertLevel.SetLevel(station.Value, alertLevel, true, true, true, false);
        }
        if (HasComp<AmbientSoundComponent>(uid))
            _ambientSound.SetSound(uid, component.CoreAmbience2);
    }
    private void PlayBackgroundSiren(EntityUid uid, EnergyCorePendingDetonationComponent corecomp)
    {
        _sound.PlayGlobalOnStation(uid, _audio.ResolveSound(corecomp.BackgroundSiren), new AudioParams { Volume = -3f });

    }
    private void PlayMeltdownMusic(EntityUid uid, EnergyCorePendingDetonationComponent corecomp)
    {
        if (!corecomp.PlayedMusic)
        {
            corecomp.PlayedMusic = true;
            _sound.PlayGlobalOnStation(uid, _audio.ResolveSound(corecomp.MeltdownMusic), new AudioParams { Volume = -2f });

            var station = _stationSystem.GetOwningStation(uid);
            if (station != null)
            {
                _chatSystem.DispatchStationAnnouncement(station.Value,
                Loc.GetString("energycore-less-2min-to-boom"),
                Loc.GetString("energy-department"),
                playDefaultSound: true,
                colorOverride: Color.Red);
            }
            if (HasComp<AmbientSoundComponent>(uid))
                _ambientSound.SetSound(uid, corecomp.CoreAmbience3);
        }
    }
    private void ExplodeCore(EntityUid uid, EnergyCorePendingDetonationComponent component,
        TransformComponent transform)
    {
        _explosion.QueueExplosion(uid, ExplosionSystem.DefaultExplosionPrototypeId, 5000000, 5, 100);
        RaiseLocalEvent(new CoreDetonatedEvent()
        {
            OwningStation = transform.GridUid,
        });
        Del(uid);
    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<EnergyCorePendingDetonationComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var trans))
        {
            if (_timing.CurTime >= comp.MusicTime)
                PlayMeltdownMusic(uid, comp);
            if (_timing.CurTime >= comp.DetonationTime)
                ExplodeCore(uid, comp, trans);
        }
    }
}
