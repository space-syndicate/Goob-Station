using Content.Server.Imperial.Power.Components;

namespace Content.Server.Imperial.Power.EntitySystems.Events;

/// <summary>
/// Событие "Ничего" - период спокойствия суперматерии
/// </summary>
public sealed class SupermatterNoneEvent
{
    public static void Activate(Entity<SupermatterEventComponent> entity, SupermatterEventSystem supermatterSystem)
    {
        if (entity.AsType() == EntityUid.Invalid)
        {
            supermatterSystem.Log.Error("SupermatterNoneEvent.Activate: Invalid EntityUid provided");
            return;
        }

        var currentTime = supermatterSystem.GameTiming.CurTime;
        entity.Comp.CurrentEvent = SupermatterEventComponent.SupermatterEventType.None;
        entity.Comp.EventEndTime = TimeSpan.Zero;
        entity.Comp.NextEventTimer = entity.Comp.NoneEventDuration;
        entity.Comp.LastEventEndTimeUpdate = currentTime;
        entity.Comp.LastNextEventTimerUpdate = currentTime;
    }

    public static void Process(Entity<SupermatterEventComponent> _, SupermatterEventSystem __, TimeSpan ___)
    {
    }

    public static string GetAnnouncement()
    {
        return Loc.GetString("supermatter-event-none");
    }
}
