using Robust.Shared.Localization;
using System;
using Content.Server.Imperial.Power.Components;

namespace Content.Server.Imperial.Power.EntitySystems.Events;

/// <summary>
/// Событие "Ничего" - период спокойствия суперматерии
/// </summary>
public sealed class SupermatterNoneEvent
{
    public void Activate(EntityUid uid, SupermatterEventComponent comp, SupermatterEventSystem system)
    {
        // Валидация входных параметров
        if (uid == EntityUid.Invalid)
        {
            system.Log.Error("SupermatterNoneEvent.Activate: Invalid EntityUid provided");
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
        if (comp.NoneEventDuration <= 0)
        {
            system.Log.Warning($"SupermatterNoneEvent.Activate: Invalid NoneEventDuration: {comp.NoneEventDuration}");
            return;
        }

        try
        {
            var currentTime = system.GameTiming.CurTime;
            comp.CurrentEvent = SupermatterEventType.None;
            comp.EventEndTime = TimeSpan.Zero;
            comp.NextEventTimer = TimeSpan.FromSeconds(comp.NoneEventDuration);
            comp.LastEventEndTimeUpdate = currentTime;
            comp.LastNextEventTimerUpdate = currentTime;
        }
        catch (Exception ex)
        {
            system.Log.Error($"SupermatterNoneEvent.Activate: Exception during activation for entity {uid}: {ex.Message}");
        }
    }

    public void Process(EntityUid uid, SupermatterEventComponent comp, SupermatterEventSystem system, TimeSpan currentTime)
    {
        // None событие не требует обработки во время выполнения
    }

    public string GetAnnouncement()
    {
        return Loc.GetString("supermatter-event-none");
    }
}
