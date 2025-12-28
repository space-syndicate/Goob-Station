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
    public static void Activate(Entity<SupermatterEventComponent> entity, SupermatterEventSystem supermatterSystem)
    {
        if (entity.AsType() == EntityUid.Invalid)
        {
            supermatterSystem.Log.Error("SupermatterPlasmaEvent.Activate: Invalid EntityUid provided");
            return;
        }

        var currentTime = supermatterSystem.GameTiming.CurTime;
        entity.Comp.CurrentEvent = SupermatterEventComponent.SupermatterEventType.Plasma;
        entity.Comp.EventEndTime = entity.Comp.PlasmaEventDuration;
        entity.Comp.NextEventTimer = entity.Comp.EventAfterPlasmaTime;
        entity.Comp.LastEventEndTimeUpdate = currentTime;
        entity.Comp.LastNextEventTimerUpdate = currentTime;
        entity.Comp.LastPlasmaTickUpdate = currentTime;
    }

    public static void Process(Entity<SupermatterEventComponent> entity, SupermatterEventSystem supermatterSystem, TimeSpan currentTime)
    {
        entity.Comp.PlasmaTickAccumulator ??= TimeSpan.Zero;

        var elapsedSinceLastUpdate = currentTime - entity.Comp.LastPlasmaTickUpdate;
        entity.Comp.PlasmaTickAccumulator += elapsedSinceLastUpdate;
        entity.Comp.LastPlasmaTickUpdate = currentTime;

        if (entity.Comp.PlasmaTickAccumulator < entity.Comp.PlasmaTickInterval)
            return;

        // Получаем компоненты один раз
        if (!supermatterSystem.TryGetComponent<TransformComponent>(entity, out var xform) || xform == null)
        {
            return;
        }

        var gas = supermatterSystem.Atmos.GetContainingMixture(entity.Owner, true);
        if (gas == null)
            return;

        // Добавляем газы
        gas.AdjustMoles((int)Gas.Plasma, entity.Comp.PlasmaMolesAmount);
        gas.AdjustMoles((int)Gas.Oxygen, entity.Comp.PlasmaMolesAmount);

        // Создаём хотспот
        if (!TryGetGridUid(xform, out var gridUid))
        {
            supermatterSystem.Log.Warning($"Supermatter plasma event triggered for entity {entity} without grid");
            return;
        }

        if (!supermatterSystem.TryGetComponent<MapGridComponent>(gridUid, out var grid) || grid == null)
            return;

        var tile = supermatterSystem.MapSystem.TileIndicesFor(gridUid, grid, xform.Coordinates);
        CreateHotspot(supermatterSystem.Atmos, gridUid, tile, entity.Comp.PlasmaHotspotTemperature, entity.Comp.PlasmaHotspotVolume, entity);

        entity.Comp.PlasmaTickAccumulator -= entity.Comp.PlasmaTickInterval;
    }

    private static bool TryGetGridUid(TransformComponent xform, out EntityUid gridUid)
    {
        gridUid = xform.GridUid ?? default;
        return xform.GridUid.HasValue;
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
