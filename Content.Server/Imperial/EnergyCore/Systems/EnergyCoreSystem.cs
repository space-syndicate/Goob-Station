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
        SubscribeLocalEvent<EnergyCoreComponent, StartCollideEvent>(SetTempLevelChange);
    }

    private void OnMapInit(EntityUid uid, EnergyCoreComponent component, MapInitEvent args)
    {
        StartCoreWork(uid, component);
    }

    private void StartCoreWork(EntityUid uid, EnergyCoreComponent component)
    {
        if (HasComp<AmbientSoundComponent>(uid))
            _ambientSound.SetSound(uid, component.CoreAmbience1);
    }
    private void SetTempLevelChange(EntityUid uid, EnergyCoreComponent component, ref StartCollideEvent args)
    {
        var other = args.OtherEntity;

        if (!_tag.HasTag(other, component.TechnicalTag))
            return;

        switch (component.ChangeTempState)
        {
            case CoreRisingChange.HEATING:
                if (_tag.HasTag(other, component.ChangeRisingTag))
                    component.ChangeTempState = CoreRisingChange.COOLING;
                component.CoreTempRise = false;
                break;
            case CoreRisingChange.COOLING:
                if (_tag.HasTag(other, component.ChangeDecreasingTag))
                    component.ChangeTempState = CoreRisingChange.HEATING;
                component.CoreTempRise = true;
                break;
        }

        switch (component.ChangeLevel)
        {
            case CoreTempChangeLevel.STANDART:
                component.TempChangeMultiplier = 300f;
                if (_tag.HasTag(other, component.TempHeaterTag) && component.ChangeLevel == CoreTempChangeLevel.STANDART)
                    component.ChangeLevel = CoreTempChangeLevel.HIGH;
                break;
            case CoreTempChangeLevel.HIGH:
                component.TempChangeMultiplier = 600f;
                if (_tag.HasTag(other, component.TempCoolerTag) && component.ChangeLevel == CoreTempChangeLevel.HIGH)
                    component.ChangeLevel = CoreTempChangeLevel.STANDART;
                break;
        }
    }
    private void OnMeltdown(EntityUid uid, EnergyCoreComponent component) // За ивент расплавления ядра отвечает отдельная система, чтобы не превращать эту в свалку
    {
        if (!HasComp<EnergyCorePendingDetonationComponent>(uid) && component.Status == CoreStatus.CATASTROPHIC)
            EnsureComp<EnergyCorePendingDetonationComponent>(uid);
        else
            return;
    }
    private void RefreshCoreStatus(EntityUid uid, EnergyCoreComponent component)
    {
        switch (component.Status)
        {
            case CoreStatus.OFFLINE: // Статус ядра: Оффлайн
                if (component.CoreTemp > 0f)
                    component.Status = CoreStatus.IDLE;
                break;

            case CoreStatus.IDLE: // Статус ядра: Простаивание
                if (component.CoreTemp > 30000f)
                    component.Status = CoreStatus.STABLE;
                if (component.CoreTemp < 0f)
                    component.Status = CoreStatus.OFFLINE;
                break;

            case CoreStatus.STABLE: // Статус ядра: Стабильный
                if (component.CoreTemp > 100000f)
                    component.Status = CoreStatus.OPTIMAL;
                if (component.CoreTemp < 30000f)
                    component.Status = CoreStatus.IDLE;
                break;

            case CoreStatus.OPTIMAL: // Статус ядра: Оптимальный
                if (component.CoreTemp > 300000f)
                    component.Status = CoreStatus.MODERATE;
                if (component.CoreTemp < 100000f)
                    component.Status = CoreStatus.STABLE;
                break;

            case CoreStatus.MODERATE: // Статус ядра: Приемлимый (повышенный оптимальный)
                if (component.CoreTemp > 600000f)
                {
                    if (component.IsSafeProtocolActive)
                        component.Status = CoreStatus.SAFE_PROTOCOL;
                    else
                        component.Status = CoreStatus.HIGH;
                }
                if (component.CoreTemp < 300000f)
                    component.Status = CoreStatus.OPTIMAL;
                break;

            case CoreStatus.HIGH: // Статус ядра: Высокая температура
                if (component.CoreTemp > 800000f)
                    component.Status = CoreStatus.CRITICAL_HIGH;
                if (component.CoreTemp < 600000f)
                    component.Status = CoreStatus.MODERATE;
                break;

            case CoreStatus.CRITICAL_HIGH: // Статус ядра: Критически высокая температура
                if (component.CoreTemp > 1000000f)
                    component.Status = CoreStatus.CATASTROPHIC;
                if (component.CoreTemp < 800000f)
                    component.Status = CoreStatus.HIGH;
                break;

            case CoreStatus.CATASTROPHIC: // Статус ядра: Катастрофически высокая температура (расплавление)
                OnMeltdown(uid, component); // Ивент расплавления ядра.
                break;

            case CoreStatus.SAFE_PROTOCOL: // Статус ядра: Протокол безопасности активен
                if (component.CoreTemp < 500000f)
                    component.Status = CoreStatus.MODERATE;
                break;

            default:
                component.Status = CoreStatus.OFFLINE; // Базовый статус ядра: Оффлайн
                break;
        }
    }
    private void UpdateCoreVisual(EntityUid uid, EnergyCoreComponent component)
    {
        if (TryComp<AppearanceComponent>(uid, out var appearance))
        {                                                              //Byte
            _appearance.SetData(uid, CoreStatusVisual.Core_Visual, (byte)component.Status, appearance);
        }
        if (TryComp<PointLightComponent>(uid, out var light)) ;
        {
            if (light != null)
            {
                switch (component.CoreColorEnum) // Это все ради света. Блять какой пиз...
                {
                    case CoreStatusColorVisual.OFFLINE: // Статус ядра: Оффлайн
                        component.CoreColor = Color.FromHex("#74aeff");
                        component.CoreColorRadius = 1f;
                        component.CoreColorEnergy = 1f;
                        if (component.Status == CoreStatus.IDLE)
                            component.CoreColorEnum = CoreStatusColorVisual.IDLE;
                        break;

                    case CoreStatusColorVisual.IDLE: // Статус ядра: Простаивание
                        component.CoreColor = Color.FromHex("#74aeff");
                        component.CoreColorRadius = 10f;
                        component.CoreColorEnergy = 2f;
                        if (component.Status == CoreStatus.STABLE)
                            component.CoreColorEnum = CoreStatusColorVisual.STABLE;
                        if (component.Status == CoreStatus.OFFLINE)
                            component.CoreColorEnum = CoreStatusColorVisual.OFFLINE;
                        break;

                    case CoreStatusColorVisual.STABLE: // Статус ядра: Стабильный
                        component.CoreColor = Color.FromHex("#d80000ff");
                        component.CoreColorRadius = 11f;
                        component.CoreColorEnergy = 3f;
                        if (component.Status == CoreStatus.OPTIMAL)
                            component.CoreColorEnum = CoreStatusColorVisual.OPTIMAL;
                        if (component.Status == CoreStatus.IDLE)
                            component.CoreColorEnum = CoreStatusColorVisual.IDLE;
                        break;

                    case CoreStatusColorVisual.OPTIMAL: // Статус ядра: Оптимальный
                        component.CoreColor = Color.FromHex("#ff0000ff");
                        component.CoreColorRadius = 11;
                        component.CoreColorEnergy = 3f;
                        if (component.Status == CoreStatus.MODERATE)
                            component.CoreColorEnum = CoreStatusColorVisual.MODERATE;
                        if (component.Status == CoreStatus.STABLE)
                            component.CoreColorEnum = CoreStatusColorVisual.STABLE;
                        break;

                    case CoreStatusColorVisual.MODERATE: // Статус ядра: Приемлимый
                        component.CoreColor = Color.FromHex("#fff700ff");
                        component.CoreColorRadius = 12f;
                        component.CoreColorEnergy = 4f;
                        if (component.Status == CoreStatus.HIGH)
                            component.CoreColorEnum = CoreStatusColorVisual.HIGH;
                        if (component.Status == CoreStatus.OPTIMAL)
                            component.CoreColorEnum = CoreStatusColorVisual.OPTIMAL;
                        break;

                    case CoreStatusColorVisual.HIGH: // Статус ядра: Высокая температура
                        component.CoreColor = Color.FromHex("#fbff11ff");
                        component.CoreColorRadius = 12f;
                        component.CoreColorEnergy = 4f;
                        if (component.Status == CoreStatus.CRITICAL_HIGH)
                            component.CoreColorEnum = CoreStatusColorVisual.CRITICAL_HIGH;
                        if (component.Status == CoreStatus.MODERATE)
                            component.CoreColorEnum = CoreStatusColorVisual.MODERATE;
                        break;

                    case CoreStatusColorVisual.CRITICAL_HIGH: // Статус ядра: Критически высокая температура
                        component.CoreColor = Color.FromHex("#fdff8fff");
                        component.CoreColorRadius = 15f;
                        component.CoreColorEnergy = 7f;
                        if (component.Status == CoreStatus.CATASTROPHIC)
                            component.CoreColorEnum = CoreStatusColorVisual.CATASTROPHIC;
                        if (component.Status == CoreStatus.HIGH)
                            component.CoreColorEnum = CoreStatusColorVisual.HIGH;
                        break;

                    case CoreStatusColorVisual.CATASTROPHIC: // Статус ядра: Катастрофически высокая температура
                        component.CoreColor = Color.FromHex("#ffffffff");
                        component.CoreColorRadius = 20f;
                        component.CoreColorEnergy = 12f;
                        break;

                    case CoreStatusColorVisual.SAFE_PROTOCOL: // Статус ядра: Протокол безопасности активен
                        component.CoreColor = Color.FromHex("#fff700ff");
                        component.CoreColorRadius = 12f;
                        component.CoreColorEnergy = 4f;
                        if (component.Status == CoreStatus.SAFE_PROTOCOL)
                            component.CoreColorEnum = CoreStatusColorVisual.SAFE_PROTOCOL;
                        if (component.Status == CoreStatus.MODERATE)
                            component.CoreColorEnum = CoreStatusColorVisual.MODERATE;
                        break;

                    default:
                        component.CoreColorEnum = CoreStatusColorVisual.OFFLINE; // Базовый статус ядра: Оффлайн
                        break;
                }
                _pointLight.SetColor(uid, component.CoreColor, light);
                _pointLight.SetRadius(uid, component.CoreColorRadius);
                _pointLight.SetEnergy(uid, component.CoreColorEnergy);
            }
        }
    }
    private void UpdateCoreTemp(EntityUid uid, EnergyCoreComponent component, float frameTime)
    {
        component.UpdateTemp = frameTime * component.TempChangeMultiplier;
        if (component.CoreTempRise) // Определяем, должна ли температура расти или уменьшаться.
        {
            component.CoreTemp += component.UpdateTemp;
        }
        else
        {
            if (component.CoreTemp < component.MinCoreTemp)
                component.CoreTemp = -899f;
            else
                component.CoreTemp -= component.UpdateTemp;
        }

        if (component.IsSafeProtocolActive)
        {
            if (component.Status == CoreStatus.SAFE_PROTOCOL) // Если температура больше 600.000 и протокол безопасности не был отключен
            {
                component.UpdateTemp = frameTime * component.TempChangeMultiplierProtocol;
                component.CoreTemp -= component.UpdateTemp;
            }
        }
        if (component.Status == CoreStatus.CATASTROPHIC) // При катастрофическом статусе контроль над ядром полностью потерян и температура ядра НЕ может быть снижена
        {
            component.UpdateTemp = frameTime * component.TempChangeMultiplierMeltdown;
            component.CoreTemp += component.UpdateTemp;
        }
    }
    private void UpdateProtocolStatus(EntityUid uid, EnergyCoreComponent component)
    {
        var nearestUid = FindNearestProtocolTerminal(uid);
        if (nearestUid == null || !TryComp(nearestUid, out CoreAccessComputerComponent? nearest))
        {
            return;
        }
        var (safeProtocol, safeProtocolCompleted) = GetTerminalProtocolStatus(nearest);

        if (safeProtocol)
            component.IsSafeProtocolActive = false;
        else
            return;
        if (!component.AnnouncedProtocol)
        {
            component.AnnouncedProtocol = true;

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
    private static (bool safeProtocol, bool safeProtocolCompleted) GetTerminalProtocolStatus(CoreAccessComputerComponent component)
    {
        var safeProtocol = component.SaveProtocolWasDeactivated;
        var safeProtocolCompleted = component.DeactivationCompleted;
        return (safeProtocol, safeProtocolCompleted);
    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<EnergyCoreComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out _))
        {
            UpdateCoreTemp(uid, comp, frameTime);
            RefreshCoreStatus(uid, comp);
            UpdateCoreVisual(uid, comp);

            var nearestUid = FindNearestProtocolTerminal(uid);
            if (nearestUid == null ||
                !EntityManager.TryGetComponent<CoreAccessComputerComponent>(nearestUid.Value, out var nearest))
                continue;

            var (safeProtocol, safeProtocolCompleted) = GetTerminalProtocolStatus(nearest);
            UpdateProtocolStatus(uid, comp);
        }
    }
}
