using Content.Server.Imperial.Power.Components;

namespace Content.Server.Imperial.Power.EntitySystems.Events;

/// <summary>
/// Событие "Радиация" - суперматерия излучает повышенную радиацию
/// </summary>
public sealed class SupermatterRadiationEvent
{
    public static void Activate(EntityUid uid, SupermatterEventComponent comp, SupermatterEventSystem supermatterSystem)
    {
        // Валидация входных параметров
        if (uid == EntityUid.Invalid)
        {
            supermatterSystem.Log.Error("SupermatterRadiationEvent.Activate: Invalid EntityUid provided");
            return;
        }
      
        var currentTime = supermatterSystem.GameTiming.CurTime;
        comp.CurrentEvent = SupermatterEventComponent.SupermatterEventType.Radiation;
        comp.EventEndTime = comp.RadiationEventDuration;
        comp.NextEventTimer = comp.EventAfterRadiationTime;
        comp.LastEventEndTimeUpdate = currentTime;
        comp.LastNextEventTimerUpdate = currentTime;

        supermatterSystem.SetRadiation(uid, comp.RadiationEventIntensity);
    }

    public static void Process(EntityUid uid, SupermatterEventComponent comp, SupermatterEventSystem supermatterSystem, TimeSpan currentTime)
    {
        supermatterSystem.SetRadiation(uid, comp.RadiationEventIntensity);
    }

    public static string GetAnnouncement()
    {
        return Loc.GetString("supermatter-event-radiation");
    }
}
