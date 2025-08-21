using Content.Server.Imperial.Power.Components;

namespace Content.Server.Imperial.Power.EntitySystems.Events;

/// <summary>
/// Событие "Ничего" - период спокойствия суперматерии
/// </summary>
public sealed class SupermatterNoneEvent
{
    public static void Activate(EntityUid uid, SupermatterEventComponent comp, SupermatterEventSystem supermatterSystem)
    {

        if (uid == EntityUid.Invalid)
        {
            supermatterSystem.Log.Error("SupermatterNoneEvent.Activate: Invalid EntityUid provided");
            return;
        }

        var currentTime = supermatterSystem.GameTiming.CurTime;
        comp.CurrentEvent = SupermatterEventComponent.SupermatterEventType.None;
        comp.EventEndTime = TimeSpan.Zero;
        comp.NextEventTimer = comp.NoneEventDuration;
        comp.LastEventEndTimeUpdate = currentTime;
        comp.LastNextEventTimerUpdate = currentTime;
    }

    public static void Process(EntityUid uid, SupermatterEventComponent comp, SupermatterEventSystem supermatterSystem, TimeSpan currentTime)
    {

    }

    public static string GetAnnouncement()
    {
        return Loc.GetString("supermatter-event-none");
    }
}
