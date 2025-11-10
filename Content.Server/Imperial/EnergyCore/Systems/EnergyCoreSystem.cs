using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Content.Server.AlertLevel;
using Content.Server.Station.Systems;
using Content.Shared.Tag;
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
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly SharedPointLightSystem _pointLight = default!;
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
            EnsureComp<EnergyCorePendingDetonationComponent>(uid);
        else
            return;
    }
    private void RefreshCoreStatus(EntityUid uid, EnergyCoreComponent core)
    {
        switch (core.Status)
        {
            case CoreStatus.OFFLINE: // Статус ядра: Оффлайн
                if (core.CoreTemp > 0f)
                    core.Status = CoreStatus.IDLE;
                break;

            case CoreStatus.IDLE: // Статус ядра: Простаивание
                if (core.CoreTemp > 30000f)
                    core.Status = CoreStatus.STABLE;
                if (core.CoreTemp < 0f)
                    core.Status = CoreStatus.OFFLINE;
                break;

            case CoreStatus.STABLE: // Статус ядра: Стабильный
                if (core.CoreTemp > 100000f)
                    core.Status = CoreStatus.OPTIMAL;
                if (core.CoreTemp < 30000f)
                    core.Status = CoreStatus.IDLE;
                break;

            case CoreStatus.OPTIMAL: // Статус ядра: Оптимальный
                if (core.CoreTemp > 300000f)
                    core.Status = CoreStatus.MODERATE;
                if (core.CoreTemp < 100000f)
                    core.Status = CoreStatus.STABLE;
                break;

            case CoreStatus.MODERATE: // Статус ядра: Приемлимый (повышенный оптимальный)
                if (core.CoreTemp > 600000f)
                {
                    if (core.IsSafeProtocolActive)
                        core.Status = CoreStatus.SAFE_PROTOCOL;
                    else
                        core.Status = CoreStatus.HIGH;
                }
                if (core.CoreTemp < 300000f)
                    core.Status = CoreStatus.OPTIMAL;
                break;

            case CoreStatus.HIGH: // Статус ядра: Высокая температура
                if (core.CoreTemp > 800000f)
                    core.Status = CoreStatus.CRITICAL_HIGH;
                if (core.CoreTemp < 600000f)
                    core.Status = CoreStatus.MODERATE;
                break;

            case CoreStatus.CRITICAL_HIGH: // Статус ядра: Критически высокая температура
                if (core.CoreTemp > 1000000f)
                    core.Status = CoreStatus.CATASTROPHIC;
                if (core.CoreTemp < 800000f)
                    core.Status = CoreStatus.HIGH;
                break;

            case CoreStatus.CATASTROPHIC: // Статус ядра: Катастрофически высокая температура (расплавление)
                OnMeltdown(uid, core); // Ивент расплавления ядра
                break;

            case CoreStatus.SAFE_PROTOCOL: // Статус ядра: Протокол безопасности активен
                if (core.CoreTemp < 500000f)
                    core.Status = CoreStatus.MODERATE;
                break;

            default:
                core.Status = CoreStatus.OFFLINE; // Базовый статус ядра: Оффлайн
                break;
        }
    }
    private void UpdateCoreVisual(EntityUid uid, EnergyCoreComponent core)
    {
        if (TryComp<AppearanceComponent>(uid, out var appearance))
        {                                                              //Byte
            _appearance.SetData(uid, CoreStatusVisual.Core_Visual, (byte)core.Status, appearance);
        }
        if (TryComp<PointLightComponent>(uid, out var light)) ;
        {
            if (light != null)
            {
                switch (core.CoreColorEnum) // Это все ради света. Блять какой пиз...
                {
                    case CoreStatusColorVisual.OFFLINE: // Статус ядра: Оффлайн
                        core.CoreColor = Color.FromHex("#74aeff");
                        core.CoreColorRadius = 1f;
                        core.CoreColorEnergy = 1f;
                        if (core.Status == CoreStatus.IDLE)
                            core.CoreColorEnum = CoreStatusColorVisual.IDLE;
                        break;

                    case CoreStatusColorVisual.IDLE: // Статус ядра: Простаивание
                        core.CoreColor = Color.FromHex("#74aeff");
                        core.CoreColorRadius = 10f;
                        core.CoreColorEnergy = 2f;
                        if (core.Status == CoreStatus.STABLE)
                            core.CoreColorEnum = CoreStatusColorVisual.STABLE;
                        if (core.Status == CoreStatus.OFFLINE)
                            core.CoreColorEnum = CoreStatusColorVisual.OFFLINE;
                        break;

                    case CoreStatusColorVisual.STABLE: // Статус ядра: Стабильный
                        core.CoreColor = Color.FromHex("#d80000ff");
                        core.CoreColorRadius = 11f;
                        core.CoreColorEnergy = 3f;
                        if (core.Status == CoreStatus.OPTIMAL)
                            core.CoreColorEnum = CoreStatusColorVisual.OPTIMAL;
                        if (core.Status == CoreStatus.IDLE)
                            core.CoreColorEnum = CoreStatusColorVisual.IDLE;
                        break;

                    case CoreStatusColorVisual.OPTIMAL: // Статус ядра: Оптимальный
                        core.CoreColor = Color.FromHex("#ff0000ff");
                        core.CoreColorRadius = 11;
                        core.CoreColorEnergy = 3f;
                        if (core.Status == CoreStatus.MODERATE)
                            core.CoreColorEnum = CoreStatusColorVisual.MODERATE;
                        if (core.Status == CoreStatus.STABLE)
                            core.CoreColorEnum = CoreStatusColorVisual.STABLE;
                        break;

                    case CoreStatusColorVisual.MODERATE: // Статус ядра: Приемлимый
                        core.CoreColor = Color.FromHex("#fff700ff");
                        core.CoreColorRadius = 12f;
                        core.CoreColorEnergy = 4f;
                        if (core.Status == CoreStatus.HIGH)
                            core.CoreColorEnum = CoreStatusColorVisual.HIGH;
                        if (core.Status == CoreStatus.OPTIMAL)
                            core.CoreColorEnum = CoreStatusColorVisual.OPTIMAL;
                        break;

                    case CoreStatusColorVisual.HIGH: // Статус ядра: Высокая температура
                        core.CoreColor = Color.FromHex("#fbff11ff");
                        core.CoreColorRadius = 12f;
                        core.CoreColorEnergy = 4f;
                        if (core.Status == CoreStatus.CRITICAL_HIGH)
                            core.CoreColorEnum = CoreStatusColorVisual.CRITICAL_HIGH;
                        if (core.Status == CoreStatus.MODERATE)
                            core.CoreColorEnum = CoreStatusColorVisual.MODERATE;
                        break;

                    case CoreStatusColorVisual.CRITICAL_HIGH: // Статус ядра: Критически высокая температура
                        core.CoreColor = Color.FromHex("#fdff8fff");
                        core.CoreColorRadius = 15f;
                        core.CoreColorEnergy = 7f;
                        if (core.Status == CoreStatus.CATASTROPHIC)
                            core.CoreColorEnum = CoreStatusColorVisual.CATASTROPHIC;
                        if (core.Status == CoreStatus.HIGH)
                            core.CoreColorEnum = CoreStatusColorVisual.HIGH;
                        break;

                    case CoreStatusColorVisual.CATASTROPHIC: // Статус ядра: Катастрофически высокая температура
                        core.CoreColor = Color.FromHex("#ffffffff");
                        core.CoreColorRadius = 20f;
                        core.CoreColorEnergy = 12f;
                        break;

                    case CoreStatusColorVisual.SAFE_PROTOCOL: // Статус ядра: Протокол безопасности активен
                        core.CoreColor = Color.FromHex("#fff700ff");
                        core.CoreColorRadius = 12f;
                        core.CoreColorEnergy = 4f;
                        if (core.Status == CoreStatus.SAFE_PROTOCOL)
                            core.CoreColorEnum = CoreStatusColorVisual.SAFE_PROTOCOL;
                        if (core.Status == CoreStatus.MODERATE)
                            core.CoreColorEnum = CoreStatusColorVisual.MODERATE;
                        break;

                    default:
                        core.CoreColorEnum = CoreStatusColorVisual.OFFLINE; // Базовый статус ядра: Оффлайн
                        break;
                }
                _pointLight.SetColor(uid, core.CoreColor, light);
                _pointLight.SetRadius(uid, core.CoreColorRadius);
                _pointLight.SetEnergy(uid, core.CoreColorEnergy);
            }
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
    private void UpdateCoreTemp(EntityUid uid, EnergyCoreComponent core, float frameTime) // YandereDev ahh moment
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
            if (core.Status == CoreStatus.SAFE_PROTOCOL) // Если температура больше 600.000 и протокол безопасности не был отключен
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
            UpdateCoreTemp(uid, cormp, frameTime);
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
