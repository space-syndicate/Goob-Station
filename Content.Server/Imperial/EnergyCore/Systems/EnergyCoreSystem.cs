using Robust.Shared.Prototypes;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;
using Content.Server.GameTicking;
using Color = Robust.Shared.Maths.Color;
using Content.Server.AlertLevel;
using Content.Server.Station.Systems;
using Content.Shared.Audio;
using Content.Server.Audio;
using Content.Server.Radio.EntitySystems;
using Content.Server.Chat.Systems;
using Content.Shared.Imperial.EnergyCore;
using Content.Shared.Imperial.EnergyCore.Components;
using Content.Server.Imperial.EnergyCore.Components;
using Content.Server.Imperial.EnergyCore.Helpers;
using Content.Server.Imperial.EnergyCore.Events;

namespace Content.Server.Imperial.EnergyCore;

public sealed partial class EnergyCoreSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly AlertLevelSystem _alertLevel = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ServerGlobalSoundSystem _sound = default!;
    [Dependency] private readonly SharedAmbientSoundSystem _ambientSound = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedPointLightSystem _pointLight = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly CoreSearchSystem _coreHelper = default!;
    public static readonly EntProtoId CoreTechnicalRule = "CoreTechnical";
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EnergyCoreComponent, MapInitEvent>(OnMapInit);
    }
    private void UpdateDataFromTerminal(EntityUid uid, EnergyCoreComponent core)
    {
        var nearestUid = core.Controller;
        if (nearestUid == null || !TryComp(nearestUid, out CoreAccessComputerComponent? nearest))
        {
            return;
        }
        var (safeProtocol, safeProtocolCompleted, tempChangeStatus, finalTempChangeCoef) = GetTerminalProtocolStatus(nearest);

        core.TempChangeMultiplier = finalTempChangeCoef;
        core.TempChangeStatus = tempChangeStatus;
    }
    private void UpdateLightVisual(EntityUid uid, string coreColor, float coreColorRadius, float coreColorEnergy)
    {
        var color = Color.FromHex(coreColor);
        if (TryComp<PointLightComponent>(uid, out var light))
        {
            _pointLight.SetColor(uid, color, light);
            _pointLight.SetRadius(uid, coreColorRadius);
            _pointLight.SetEnergy(uid, coreColorEnergy);
        }
    }
    private void UpdateCoreVisual(EntityUid uid, EnergyCoreComponent core)
    {
        if (TryComp<AppearanceComponent>(uid, out var appearance))
        {                                                              //Byte
            _appearance.SetData(uid, CoreStatusVisual.Core_Visual, (byte)core.Status, appearance);
        }
    }
    private void UpdateCoreTemp(EnergyCoreComponent core, float frameTime) // YandereDev ahh moment
    {
        core.UpdateTemp = frameTime * core.TempChangeMultiplier;

        switch (core.TempRiseStatus)
        {
            case CoreTempChangeLevel.COOLING: // Охлаждение
                core.CoreTemp -= core.UpdateTemp;
                if (core.CoreTemp < core.MinCoreTemp)
                    core.CoreTemp = core.MinCoreTemp;
                break;

            case CoreTempChangeLevel.AUTO: // Авто режим активен
                if (core.CoreTemp < 250000)
                    core.CoreTemp += core.UpdateTemp;
                else
                    core.CoreTemp -= core.UpdateTemp;
                break;

            case CoreTempChangeLevel.HEATING: // Нагревание
                core.CoreTemp += core.UpdateTemp;
                break;

            default:
                core.TempRiseStatus = CoreTempChangeLevel.COOLING;
                break;
        }

        if (core.IsSafeProtocolActive)
        {
            if (core.Status == CoreStatus.SAFEPROTOCOL) // Если температура больше 600.000 и протокол безопасности не был отключен
            {
                core.UpdateTemp = frameTime * core.TempChangeMultiplierProtocol;
                core.CoreTemp -= core.UpdateTemp;
            }
        }
        if (core.Status == CoreStatus.CATASTROPHIC) // При катастрофическом статусе контроль над ядром полностью потерян и температура ядра НЕ может быть снижена
        {
            core.UpdateTemp = frameTime * core.TempChangeMultiplierMeltdown;
            core.CoreTemp += core.UpdateTemp;
        }
    }
    private void UpdateProtocolStatus(EntityUid uid, EnergyCoreComponent core)
    {
        var nearestUid = core.Controller;
        if (nearestUid == null || !TryComp(nearestUid, out CoreAccessComputerComponent? nearest))
        {
            return;
        }
        var (safeProtocol, safeProtocolCompleted, tempChangeStatus, finalTempChangeCoef) = GetTerminalProtocolStatus(nearest);

        if (safeProtocol)
            core.IsSafeProtocolActive = false;
        else
            return;

        if (core.AnnouncedProtocol) return;

        core.AnnouncedProtocol = true;

        var station = _stationSystem.GetOwningStation(uid);
        if (station == null) return;

        _alertLevel.SetLevel(station.Value, "red", true, true, true);
        _chatSystem.DispatchStationAnnouncement(station.Value,
        Loc.GetString("energycore-protocol-deactivated"), Loc.GetString("energy-department"),
        playDefaultSound: false, colorOverride: Color.Red);
    }
    private void OnMapInit(EntityUid uid, EnergyCoreComponent core, MapInitEvent args)
    {
        StartCoreWork(uid, core);

        core.SearchTime = core.SearchTime + _timing.CurTime;
    }

    private void StartCoreWork(EntityUid uid, EnergyCoreComponent core)
    {
        if (HasComp<AmbientSoundComponent>(uid))
            _ambientSound.SetSound(uid, core.CoreAmbience1);
    }
    private void OnMeltdown(EntityUid uid, EnergyCoreComponent core) // За ивент расплавления ядра отвечает отдельная система, чтобы не превращать эту в свалку
    {
        if (!HasComp<EnergyCorePendingDetonationComponent>(uid) && core.Status == CoreStatus.CATASTROPHIC)
        {
            _gameTicker.AddGameRule(CoreTechnicalRule);
            EnsureComp<EnergyCorePendingDetonationComponent>(uid);
        }
        else
            return;
    }
    private void RefreshCoreStatus(EntityUid uid, EnergyCoreComponent core)
    {
        switch (core.Status)
        {
            case CoreStatus.OFFLINE: // Статус ядра: Оффлайн
                UpdateLightVisual(uid, core.OfflineColor, 1f, 1f);
                if (core.CoreTemp > 1f)
                {
                    core.Status = CoreStatus.IDLE;
                    UpdateCoreVisual(uid, core);
                }
                break;

            case CoreStatus.IDLE: // Статус ядра: Простаивание
                UpdateLightVisual(uid, core.IdleColor, 10f, 2f);
                if (core.CoreTemp > 30000f)
                {
                    core.Status = CoreStatus.STABLE;
                    UpdateCoreVisual(uid, core);
                }
                if (core.CoreTemp <= 0f)
                {
                    core.Status = CoreStatus.OFFLINE;
                    UpdateCoreVisual(uid, core);
                }
                break;

            case CoreStatus.STABLE: // Статус ядра: Стабильный
                UpdateLightVisual(uid, core.StableColor, 11f, 3f);
                if (core.CoreTemp > 100000f)
                {
                    core.Status = CoreStatus.OPTIMAL;
                    UpdateCoreVisual(uid, core);
                }
                if (core.CoreTemp < 30000f)
                {
                    core.Status = CoreStatus.IDLE;
                    UpdateCoreVisual(uid, core);
                }
                break;

            case CoreStatus.OPTIMAL: // Статус ядра: Оптимальный
                UpdateLightVisual(uid, core.OptimalColor, 11f, 3f);
                if (core.CoreTemp > 300000f)
                {
                    core.Status = CoreStatus.MODERATE;
                    UpdateCoreVisual(uid, core);
                }
                if (core.CoreTemp < 100000f)
                {
                    core.Status = CoreStatus.STABLE;
                    UpdateCoreVisual(uid, core);
                }
                break;

            case CoreStatus.MODERATE: // Статус ядра: Приемлимый (повышенный оптимальный)
                UpdateLightVisual(uid, core.ModerateColor, 12f, 4f);
                if (core.CoreTemp > 600000f)
                {
                    if (core.IsSafeProtocolActive)
                    {
                        core.Status = CoreStatus.SAFEPROTOCOL;
                        UpdateCoreVisual(uid, core);
                    }
                    else
                    {
                        core.Status = CoreStatus.HIGH;
                        UpdateCoreVisual(uid, core);
                    }
                }
                if (core.CoreTemp < 300000f)
                {
                    core.Status = CoreStatus.OPTIMAL;
                    UpdateCoreVisual(uid, core);
                }
                break;

            case CoreStatus.HIGH: // Статус ядра: Высокая температура
                UpdateLightVisual(uid, core.HighColor, 12f, 4f);
                if (core.CoreTemp > 800000f)
                {
                    core.Status = CoreStatus.CRITICALHIGH;
                    UpdateCoreVisual(uid, core);
                    SendCriticalAnnounce(uid, core); // Сообщение
                }
                if (core.CoreTemp < 600000f)
                {
                    core.Status = CoreStatus.MODERATE;
                    UpdateCoreVisual(uid, core);
                }
                break;

            case CoreStatus.CRITICALHIGH: // Статус ядра: Критически высокая температура
                UpdateLightVisual(uid, core.CriticalHighColor, 15f, 7f);
                if (core.CoreTemp > 1000000f)
                {
                    core.Status = CoreStatus.CATASTROPHIC;
                    UpdateCoreVisual(uid, core);
                }
                if (core.CoreTemp < 800000f)
                {
                    core.Status = CoreStatus.HIGH;
                    UpdateCoreVisual(uid, core);
                }
                break;

            case CoreStatus.CATASTROPHIC: // Статус ядра: Катастрофически высокая температура (расплавление)
                UpdateLightVisual(uid, core.CompromisedColor, 20f, 12f);
                OnMeltdown(uid, core); // Ивент расплавления ядра
                break;

            case CoreStatus.SAFEPROTOCOL: // Статус ядра: Протокол безопасности активен
                UpdateLightVisual(uid, core.ProtocolColor, 12f, 4f);
                if (core.CoreTemp < 500000f)
                    core.Status = CoreStatus.MODERATE;
                break;

            default:
                core.Status = CoreStatus.OFFLINE; // Базовый статус ядра: Оффлайн
                break;
        }
    }
    private void SendCriticalAnnounce(EntityUid uid, EnergyCoreComponent core)
    {
        if(!core.AnnounceReady) return;

        var station = _stationSystem.GetOwningStation(uid);

        if (station == null) return;
        _chatSystem.DispatchStationAnnouncement(station.Value,
        Loc.GetString("energycore-critical-high-announce"), Loc.GetString("energy-department"),
        playDefaultSound: false, colorOverride: Color.DarkSalmon);
        _sound.PlayGlobalOnStation(uid, _audio.ResolveSound(core.CriticalHighAnnounce));
        core.AnnounceReady = false;
    }
    private void CheckTempChangeValue(EnergyCoreComponent core)
    {
        switch (core.TempChangeStatus)
        {
            case 1:
                core.TempRiseStatus = CoreTempChangeLevel.COOLING;
                break;
            case 2:
                core.TempRiseStatus = CoreTempChangeLevel.AUTO;
                break;
            case 3:
                core.TempRiseStatus = CoreTempChangeLevel.HEATING;
                break;
        }
    }
    private (bool safeProtocol, bool safeProtocolCompleted, byte tempChangeStatus, float finalTempChangeCoef) GetTerminalProtocolStatus(CoreAccessComputerComponent core)
    {
        var safeProtocol = core.SaveProtocolWasDeactivated;
        var safeProtocolCompleted = core.DeactivationCompleted;
        var tempChangeStatus = core.ByteStatus;
        var finalTempChangeCoef = core.FinalTempChangeCoef;
        return (safeProtocol, safeProtocolCompleted, tempChangeStatus, finalTempChangeCoef);
    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<EnergyCoreComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var cormp, out _))
        {
            CheckTempChangeValue(cormp);
            UpdateCoreTemp(cormp, frameTime);
            RefreshCoreStatus(uid, cormp);
            UpdateProtocolStatus(uid, cormp);
            UpdateDataFromTerminal(uid, cormp);

            if (_timing.CurTime < cormp.SearchTime) // Ищет только первые 5 секунд
            {
                var nearestUid = _coreHelper.FindNearestProtocolTerminal(uid, 30f);
                if (nearestUid == null ||
                    !EntityManager.TryGetComponent<CoreAccessComputerComponent>(nearestUid.Value, out var nearest))
                    continue;

                cormp.Controller = nearestUid;
            }
        }
    }
    #region public API

    public void RestoreCore(EntityUid uid, bool announce, EnergyCoreComponent? core = null)
    {
        if (!Resolve(uid, ref core))
            return;

        _metaData.SetEntityName(uid, Loc.GetString("energycore-name"));
        _metaData.SetEntityDescription(uid, Loc.GetString("energycore-desc"));
        _ambientSound.SetSound(uid, core.CoreAmbience1);

        core.CoreTemp = 0;
        core.Status = CoreStatus.OFFLINE;

        if (!core.IsSafeProtocolActive && core.AnnouncedProtocol)
        {
            core.IsSafeProtocolActive = true;
            core.AnnouncedProtocol = false;
        }

        if (TryComp(uid, out EnergyCorePendingDetonationComponent? pending))
            RemComp(uid, pending);

        if (announce)
        {
            var station = _stationSystem.GetOwningStation(uid);
            if (station != null)
            {
                _alertLevel.SetLevel(station.Value, "green", true, true, true);
                _chatSystem.DispatchStationAnnouncement(station.Value,
                Loc.GetString("energycore-have-a-good-day"), Loc.GetString("energy-department"),
                playDefaultSound: false, colorOverride: Color.DarkSalmon);
            }
        }
        UpdateCoreVisual(uid, core);
    }
    public void Corearm(EntityUid uid, EnergyCoreComponent? core = null)
    {
        if (!Resolve(uid, ref core))
            return;

        core.IsSafeProtocolActive = false;
        core.CoreTemp = 1010000f;
    }
    #endregion
}
