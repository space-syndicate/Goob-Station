using Content.Server.Imperial.Power.Components;

namespace Content.Server.Imperial.Power.EntitySystems.Events;

/// <summary>
/// Событие "Радиация" - суперматерия излучает повышенную радиацию
/// </summary>
public sealed class SupermatterRadiationEvent
{
    public static void Activate(Entity<SupermatterEventComponent> entity, SupermatterEventSystem supermatterSystem)
    {
        // Валидация входных параметров
        if (entity.AsType() == EntityUid.Invalid)
        {
            supermatterSystem.Log.Error("SupermatterRadiationEvent.Activate: Invalid EntityUid provided");
            return;
        }

        var currentTime = supermatterSystem.GameTiming.CurTime;
        entity.Comp.CurrentEvent = SupermatterEventComponent.SupermatterEventType.Radiation;
        entity.Comp.EventEndTime = entity.Comp.RadiationEventDuration;
        entity.Comp.NextEventTimer = entity.Comp.EventAfterRadiationTime;
        entity.Comp.LastEventEndTimeUpdate = currentTime;
        entity.Comp.LastNextEventTimerUpdate = currentTime;

        supermatterSystem.SetRadiation(entity, entity.Comp.RadiationEventIntensity);
    }

    public static void Process(Entity<SupermatterEventComponent> entity, SupermatterEventSystem supermatterSystem, TimeSpan currentTime)
    {
        supermatterSystem.SetRadiation(entity, entity.Comp.RadiationEventIntensity);
    }

    public static string GetAnnouncement()
    {
        return Loc.GetString("supermatter-event-radiation");
    }
}
