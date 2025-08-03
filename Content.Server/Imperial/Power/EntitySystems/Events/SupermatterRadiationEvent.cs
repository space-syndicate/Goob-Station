using Robust.Shared.Localization;
using System;
using Content.Server.Imperial.Power.Components;

namespace Content.Server.Imperial.Power.EntitySystems.Events;

/// <summary>
/// Событие "Радиация" - суперматерия излучает повышенную радиацию
/// </summary>
public sealed class SupermatterRadiationEvent
{
    public void Activate(EntityUid uid, SupermatterEventComponent comp, SupermatterEventSystem system)
    {
        // Валидация входных параметров
        if (uid == EntityUid.Invalid)
        {
            system.Log.Error("SupermatterRadiationEvent.Activate: Invalid EntityUid provided");
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
        if (comp.RadiationEventDuration <= 0)
        {
            system.Log.Warning($"SupermatterRadiationEvent.Activate: Invalid RadiationEventDuration: {comp.RadiationEventDuration}");
            return;
        }

        if (comp.RadiationMinNextEvent <= 0 || comp.RadiationMaxNextEvent <= 0)
        {
            system.Log.Warning($"SupermatterRadiationEvent.Activate: Invalid next event range: min={comp.RadiationMinNextEvent}, max={comp.RadiationMaxNextEvent}");
            return;
        }

        if (comp.RadiationMinNextEvent > comp.RadiationMaxNextEvent)
        {
            system.Log.Warning($"SupermatterRadiationEvent.Activate: Min next event time greater than max: min={comp.RadiationMinNextEvent}, max={comp.RadiationMaxNextEvent}");
            return;
        }

        if (comp.RadiationIntensity <= 0)
        {
            system.Log.Warning($"SupermatterRadiationEvent.Activate: Invalid RadiationIntensity: {comp.RadiationIntensity}");
            return;
        }

        try
        {
            var currentTime = system.GameTiming.CurTime;
            comp.CurrentEvent = SupermatterEventType.Radiation;
            comp.EventEndTime = TimeSpan.FromSeconds(comp.RadiationEventDuration);
            comp.NextEventTimer = TimeSpan.FromSeconds(system.Random.NextFloat(comp.RadiationMinNextEvent, comp.RadiationMaxNextEvent));
            comp.LastEventEndTimeUpdate = currentTime;
            comp.LastNextEventTimerUpdate = currentTime;

            // Обработка потенциальных исключений при установке радиации
            system.SetRadiation(uid, comp.RadiationIntensity);
        }
        catch (Exception ex)
        {
            system.Log.Error($"SupermatterRadiationEvent.Activate: Exception during activation for entity {uid}: {ex.Message}");
        }
    }

    public void Process(EntityUid uid, SupermatterEventComponent comp, SupermatterEventSystem system, TimeSpan currentTime)
    {
        // Поддерживаем радиацию на заданном уровне
        system.SetRadiation(uid, comp.RadiationIntensity);
    }

    public string GetAnnouncement()
    {
        return Loc.GetString("supermatter-event-radiation");
    }
}
