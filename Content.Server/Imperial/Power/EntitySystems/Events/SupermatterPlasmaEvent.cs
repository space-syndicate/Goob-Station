using Content.Shared.Atmos;
using Content.Server.Imperial.Power.Components;
using Robust.Shared.Map.Components;
using Content.Server.Atmos.EntitySystems;

namespace Content.Server.Imperial.Power.EntitySystems.Events;

/// <summary>
/// Событие "Плазма" - суперматерия генерирует плазму
/// </summary>
public sealed class SupermatterPlasmaEvent
{
    public static void Activate(EntityUid uid, SupermatterEventComponent comp, SupermatterEventSystem system)
    {
        // Валидация входных параметров
        if (uid == EntityUid.Invalid)
        {
            system.Log.Error("SupermatterPlasmaEvent.Activate: Invalid EntityUid provided");
            return;
        }

        var currentTime = system.GameTiming.CurTime;
        comp.CurrentEvent = SupermatterEventComponent.SupermatterEventType.Plasma;
        comp.EventEndTime = comp.PlasmaEventDuration;
        comp.NextEventTimer = comp.EventAfterPlasmaTime;
        comp.LastEventEndTimeUpdate = currentTime;
        comp.LastNextEventTimerUpdate = currentTime;
        comp.LastPlasmaTickUpdate = currentTime;
    }

    public static void Process(EntityUid uid, SupermatterEventComponent comp, SupermatterEventSystem system, TimeSpan currentTime)
    {
        comp.PlasmaTickAccumulator ??= TimeSpan.Zero;

        var elapsedSinceLastUpdate = currentTime - comp.LastPlasmaTickUpdate;
        comp.PlasmaTickAccumulator += elapsedSinceLastUpdate;
        comp.LastPlasmaTickUpdate = currentTime;

        if (comp.PlasmaTickAccumulator < comp.PlasmaTickInterval)
            return;

        // Получаем компоненты один раз
        if (!system.TryGetComponent<TransformComponent>(uid, out var transformComponent) || transformComponent == null)
        {
            return;
        }

        var gas = system.Atmos.GetContainingMixture(uid, true);
        if (gas == null)
            return;

        // Добавляем газы
        gas.AdjustMoles((int)Gas.Plasma, comp.PlasmaMolesAmount);
        gas.AdjustMoles((int)Gas.Oxygen, comp.PlasmaMolesAmount);

        // Создаём хотспот
        if (!TryGetGridUid(transformComponent, out var gridUid))
        {
            system.Log.Warning($"Supermatter plasma event triggered for entity {uid} without grid");
            return;
        }

        if (!system.TryGetComponent<MapGridComponent>(gridUid, out var grid) || grid == null)
            return;

        var tile = system.MapSystem.TileIndicesFor(gridUid, grid, transformComponent.Coordinates);
        CreateHotspot(system.Atmos, gridUid, tile, comp.PlasmaHotspotTemperature, comp.PlasmaHotspotVolume, uid);

        comp.PlasmaTickAccumulator -= comp.PlasmaTickInterval;
    }

    private static bool TryGetGridUid(TransformComponent transformComponent, out EntityUid gridUid)
    {
        gridUid = transformComponent.GridUid ?? default;
        return transformComponent.GridUid.HasValue;
    }

    private static void CreateHotspot(AtmosphereSystem atmos, EntityUid gridUid, Vector2i tile, float temp, float volume, EntityUid uid)
    {
        atmos.HotspotExpose(gridUid, tile, temp, volume, uid, true);
    }

    public static string GetAnnouncement()
    {
        return Loc.GetString("supermatter-event-plasma");
    }
}
