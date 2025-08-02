using Robust.Shared.Localization;
using System;
using Content.Shared.Atmos;
using Content.Server.Imperial.Power.Components;
using Robust.Shared.Map.Components;
using Robust.Shared.Map;
using Content.Server.Atmos.EntitySystems;

namespace Content.Server.Imperial.Power.EntitySystems.Events;

/// <summary>
/// Событие "Плазма" - суперматерия генерирует плазму
/// </summary>
public sealed class SupermatterPlasmaEvent : ISupermatterEvent
{
    public void Activate(EntityUid uid, SupermatterEventComponent comp, SupermatterEventSystem system)
    {
        // Валидация входных параметров
        if (uid == EntityUid.Invalid)
        {
            system.Log.Error("SupermatterPlasmaEvent.Activate: Invalid EntityUid provided");
            return;
        }

        if (comp == null)
        {
            return;
        }

        if (system == null)
        {
            return;
        }

        // Валидация конфигурации компонента
        if (comp.PlasmaEventDuration <= 0)
        {
            system.Log.Warning($"SupermatterPlasmaEvent.Activate: Invalid PlasmaEventDuration: {comp.PlasmaEventDuration}");
            return;
        }

        if (comp.PlasmaMinNextEvent <= 0 || comp.PlasmaMaxNextEvent <= 0)
        {
            system.Log.Warning($"SupermatterPlasmaEvent.Activate: Invalid next event range: min={comp.PlasmaMinNextEvent}, max={comp.PlasmaMaxNextEvent}");
            return;
        }

        if (comp.PlasmaMinNextEvent > comp.PlasmaMaxNextEvent)
        {
            system.Log.Warning($"SupermatterPlasmaEvent.Activate: Min next event time greater than max: min={comp.PlasmaMinNextEvent}, max={comp.PlasmaMaxNextEvent}");
            return;
        }

        if (comp.PlasmaTickInterval <= 0)
        {
            system.Log.Warning($"SupermatterPlasmaEvent.Activate: Invalid PlasmaTickInterval: {comp.PlasmaTickInterval}");
            return;
        }

        if (comp.PlasmaMolesAmount <= 0)
        {
            system.Log.Warning($"SupermatterPlasmaEvent.Activate: Invalid PlasmaMolesAmount: {comp.PlasmaMolesAmount}");
            return;
        }

        if (comp.PlasmaHotspotTemperature <= 0)
        {
            system.Log.Warning($"SupermatterPlasmaEvent.Activate: Invalid PlasmaHotspotTemperature: {comp.PlasmaHotspotTemperature}");
            return;
        }

        if (comp.PlasmaHotspotVolume <= 0)
        {
            system.Log.Warning($"SupermatterPlasmaEvent.Activate: Invalid PlasmaHotspotVolume: {comp.PlasmaHotspotVolume}");
            return;
        }

        try
        {
            var currentTime = system.GameTiming.CurTime;
            comp.CurrentEvent = SupermatterEventType.Plasma;
            comp.EventEndTime = TimeSpan.FromSeconds(comp.PlasmaEventDuration);
            comp.NextEventTimer = TimeSpan.FromSeconds(system.Random.NextFloat(comp.PlasmaMinNextEvent, comp.PlasmaMaxNextEvent));
            comp.LastEventEndTimeUpdate = currentTime;
            comp.LastNextEventTimerUpdate = currentTime;
            comp.LastPlasmaTickUpdate = currentTime;
        }
        catch (Exception ex)
        {
            system.Log.Error($"SupermatterPlasmaEvent.Activate: Exception during activation for entity {uid}: {ex.Message}");
        }
    }

    public void Process(EntityUid uid, SupermatterEventComponent comp, SupermatterEventSystem system, TimeSpan currentTime)
    {
        if (!comp.PlasmaTickAccumulator.HasValue)
            comp.PlasmaTickAccumulator = TimeSpan.Zero;

        var elapsedSinceLastUpdate = currentTime - comp.LastPlasmaTickUpdate;
        comp.PlasmaTickAccumulator += elapsedSinceLastUpdate;
        comp.LastPlasmaTickUpdate = currentTime;

        if (comp.PlasmaTickAccumulator < TimeSpan.FromSeconds(comp.PlasmaTickInterval))
            return;

        // Получаем компоненты один раз
        if (!system.TryGetComponent<TransformComponent>(uid, out var xform) || xform == null)
        {
            return;
        }

        if (system.Atmos == null)
            return;

        var gas = system.Atmos.GetContainingMixture(uid, true, false);
        if (gas == null)
            return;

        // Добавляем газы
        gas.AdjustMoles((int)Gas.Plasma, comp.PlasmaMolesAmount);
        gas.AdjustMoles((int)Gas.Oxygen, comp.PlasmaMolesAmount);

        // Создаём хотспот
        if (!TryGetGridUid(xform, out var gridUid))
        {
            system.Log.Warning($"Supermatter plasma event triggered for entity {uid} without grid");
            return;
        }
        if (!system.TryGetComponent<MapGridComponent>(gridUid, out var grid) || grid == null)
        {
            return;
        }
        var tile = system.MapSystem!.TileIndicesFor(gridUid, grid, xform.Coordinates);
        CreateHotspot(system.Atmos!, gridUid, tile, comp.PlasmaHotspotTemperature, comp.PlasmaHotspotVolume, uid);

        comp.PlasmaTickAccumulator -= TimeSpan.FromSeconds(comp.PlasmaTickInterval);
    }

    private bool TryGetGridUid(TransformComponent xform, out EntityUid gridUid)
    {
        if (!xform.GridUid.HasValue)
        {
            gridUid = default;
            return false;
        }
        gridUid = xform.GridUid.Value;
        return true;
    }

    private void CreateHotspot(AtmosphereSystem atmos, EntityUid gridUid, Vector2i tile, float temp, float volume, EntityUid uid)
    {
        atmos.HotspotExpose(gridUid, tile, temp, volume, uid, true);
    }

    public string GetAnnouncement()
    {
        return Loc.GetString("supermatter-event-plasma");
    }
}
