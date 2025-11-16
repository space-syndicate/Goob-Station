using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Content.Server.GameTicking;
using Color = Robust.Shared.Maths.Color;
using Content.Server.AlertLevel;
using Content.Server.Station.Systems;
using Content.Shared.Audio;
using Content.Server.Radio.EntitySystems;
using Content.Server.Chat.Systems;
using Content.Shared.Audio;
using Content.Shared.Imperial.EnergyCore;
using Content.Server.Imperial.EnergyCore.Components;

namespace Content.Server.Imperial.EnergyCore;

public sealed class EnergyCoreSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly AlertLevelSystem _alertLevel = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedAmbientSoundSystem _ambientSound = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedPointLightSystem _pointLight = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EnergyCoreComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, EnergyCoreComponent core, MapInitEvent args)
    {
        StartCoreWork(uid, core);
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
            _gameTicker.AddGameRule("CoreTechnical");
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
                UpdateLightVisual(uid, "#74aeff", 1f, 1f);
                if (core.CoreTemp > 0f)
                    core.Status = CoreStatus.IDLE;
                break;

            case CoreStatus.IDLE: // Статус ядра: Простаивание
                UpdateLightVisual(uid, "#74aeff", 10f, 2f);
                if (core.CoreTemp > 30000f)
                    core.Status = CoreStatus.STABLE;
                if (core.CoreTemp < 0f)
                    core.Status = CoreStatus.OFFLINE;
                break;

            case CoreStatus.STABLE: // Статус ядра: Стабильный
                UpdateLightVisual(uid, "#d80000ff", 11f, 3f);
                if (core.CoreTemp > 100000f)
                    core.Status = CoreStatus.OPTIMAL;
                if (core.CoreTemp < 30000f)
                    core.Status = CoreStatus.IDLE;
                break;

            case CoreStatus.OPTIMAL: // Статус ядра: Оптимальный
                UpdateLightVisual(uid, "#ff0000ff", 11f, 3f);
                if (core.CoreTemp > 300000f)
                    core.Status = CoreStatus.MODERATE;
                if (core.CoreTemp < 100000f)
                    core.Status = CoreStatus.STABLE;
                break;

            case CoreStatus.MODERATE: // Статус ядра: Приемлимый (повышенный оптимальный)
                UpdateLightVisual(uid, "#fff700ff", 12f, 4f);
                if (core.CoreTemp > 600000f)
                {
                    if (core.IsSafeProtocolActive)
                        core.Status = CoreStatus.SAFEPROTOCOL;
                    else
                        core.Status = CoreStatus.HIGH;
                }
                if (core.CoreTemp < 300000f)
                    core.Status = CoreStatus.OPTIMAL;
                break;

            case CoreStatus.HIGH: // Статус ядра: Высокая температура
                UpdateLightVisual(uid, "#fbff11ff", 12f, 4f);
                if (core.CoreTemp > 800000f)
                    core.Status = CoreStatus.CRITICALHIGH;
                if (core.CoreTemp < 600000f)
                    core.Status = CoreStatus.MODERATE;
                break;

            case CoreStatus.CRITICALHIGH: // Статус ядра: Критически высокая температура
                UpdateLightVisual(uid, "#fdff8fff", 15f, 7f);
                if (core.CoreTemp > 1000000f)
                    core.Status = CoreStatus.CATASTROPHIC;
                if (core.CoreTemp < 800000f)
                    core.Status = CoreStatus.HIGH;
                break;

            case CoreStatus.CATASTROPHIC: // Статус ядра: Катастрофически высокая температура (расплавление)
                UpdateLightVisual(uid, "#ffffffff", 20f, 12f);
                OnMeltdown(uid, core); // Ивент расплавления ядра
                break;

            case CoreStatus.SAFEPROTOCOL: // Статус ядра: Протокол безопасности активен
                UpdateLightVisual(uid, "#fbff11ff", 12f, 4f);
                if (core.CoreTemp < 500000f)
                    core.Status = CoreStatus.MODERATE;
                break;

            default:
                core.Status = CoreStatus.OFFLINE; // Базовый статус ядра: Оффлайн
                break;
        }
    }
    private void UpdateLightVisual(EntityUid uid, string coreColor, float coreColorRadius, float coreColorEnergy)
    {
        var color = Color.FromHex(coreColor);
        if (TryComp<PointLightComponent>(uid, out var light)) ;
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
    private void CheckTempChangeValue(EntityUid uid, EnergyCoreComponent core)
    {
        var cooling = 1;
        var auto = 2;
        var heating = 3;

        if (core.TempChangeStatus == (byte)cooling)
            core.TempRiseStatus = CoreTempChangeLevel.COOLING;
        if (core.TempChangeStatus == (byte)auto)
            core.TempRiseStatus = CoreTempChangeLevel.AUTO;
        if (core.TempChangeStatus == (byte)heating)
            core.TempRiseStatus = CoreTempChangeLevel.HEATING;
    }
    private void UpdateCoreTemp(EnergyCoreComponent core, float frameTime) // YandereDev ahh moment
    {
        core.UpdateTemp = frameTime * core.TempChangeMultiplier;

        switch (core.TempRiseStatus)
        {
            case CoreTempChangeLevel.COOLING: // Охлаждение
                core.CoreTemp -= core.UpdateTemp;
                if (core.CoreTemp < core.MinCoreTemp)
                    core.CoreTemp = -899f;
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
        var nearestUid = FindNearestProtocolTerminal(uid);
        if (nearestUid == null || !TryComp(nearestUid, out CoreAccessComputerComponent? nearest))
        {
            return;
        }
        var (safeProtocol, safeProtocolCompleted, tempChangeStatus, finalTempChangeCoef) = GetTerminalProtocolStatus(nearest);

        if (safeProtocol)
            core.IsSafeProtocolActive = false;
        else
            return;
        if (!core.AnnouncedProtocol)
        {
            core.AnnouncedProtocol = true;

            var station = _stationSystem.GetOwningStation(uid);
            if (station != null)
            {
                _alertLevel.SetLevel(station.Value, "red", true, true, true);
                _chatSystem.DispatchStationAnnouncement(station.Value,
                Loc.GetString("energycore-protocol-deactivated"), Loc.GetString("energy-department"),
                playDefaultSound: true, colorOverride: Color.Red);
            }
        }

    }
    private EntityUid? FindNearestProtocolTerminal(EntityUid terminal)
    {
        var transformCompConsole = Transform(terminal);
        var mapId = transformCompConsole.MapID;
        var pos = _transformSystem.GetMapCoordinates(transformCompConsole).Position;

        EntityUid? nearest = null;
        var minDist = float.MaxValue;

        var enumerator = EntityQueryEnumerator<CoreAccessComputerComponent, TransformComponent>();
        while (enumerator.MoveNext(out var uid, out _, out var transComp))
        {
            if (transComp.MapID != mapId)
                continue;

            var corepos = _transformSystem.GetMapCoordinates(uid).Position;
            var dist = (corepos - pos).LengthSquared();
            if (dist > minDist)
                continue;

            minDist = dist;
            nearest = uid;
        }
        return nearest;
    }
    private void UpdateDataFromTerminal(EntityUid uid, EnergyCoreComponent core)
    {
        var nearestUid = FindNearestProtocolTerminal(uid);
        if (nearestUid == null || !TryComp(nearestUid, out CoreAccessComputerComponent? nearest))
        {
            return;
        }
        var (safeProtocol, safeProtocolCompleted, tempChangeStatus, finalTempChangeCoef) = GetTerminalProtocolStatus(nearest);

        core.TempChangeMultiplier = finalTempChangeCoef;
        core.TempChangeStatus = tempChangeStatus;
    }
    private static (bool safeProtocol, bool safeProtocolCompleted, byte tempChangeStatus, float finalTempChangeCoef) GetTerminalProtocolStatus(CoreAccessComputerComponent core)
    {
        var safeProtocol = core.SaveProtocolWasDeactivated;
        var safeProtocolCompleted = core.DeactivationCompleted;
        var coreTempRising = core.TempRising;
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
            CheckTempChangeValue(uid, cormp);
            UpdateCoreTemp(cormp, frameTime);
            RefreshCoreStatus(uid, cormp);
            UpdateCoreVisual(uid, cormp);

            var nearestUid = FindNearestProtocolTerminal(uid);
            if (nearestUid == null ||
                !EntityManager.TryGetComponent<CoreAccessComputerComponent>(nearestUid.Value, out var nearest))
                continue;

            var (safeProtocol, safeProtocolCompleted, tempChangeStatus, finalTempChangeCoef) = GetTerminalProtocolStatus(nearest);
            UpdateProtocolStatus(uid, cormp);
            UpdateDataFromTerminal(uid, cormp);
        }
    }
}
