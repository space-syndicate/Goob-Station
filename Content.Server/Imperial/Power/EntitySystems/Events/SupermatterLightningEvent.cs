using Robust.Shared.Localization;
using System;
using Content.Server.Imperial.Power.Components;
using Content.Server.Imperial.ImperialLightning;
using Content.Shared.Damage;

namespace Content.Server.Imperial.Power.EntitySystems.Events;

/// <summary>
/// Событие "Молния" - суперматерия генерирует электрические разряды
/// </summary>
public sealed class SupermatterLightningEvent : ISupermatterEvent
{
    public void Activate(EntityUid uid, SupermatterEventComponent comp, SupermatterEventSystem system)
    {
        // Валидация входных параметров
        if (uid == EntityUid.Invalid)
        {
            system.Log.Error("SupermatterLightningEvent.Activate: Invalid EntityUid provided");
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
        if (comp.LightningEventDuration <= 0)
        {
            system.Log.Warning($"SupermatterLightningEvent.Activate: Invalid LightningEventDuration: {comp.LightningEventDuration}");
            return;
        }

        if (comp.LightningMinNextEvent <= 0 || comp.LightningMaxNextEvent <= 0)
        {
            system.Log.Warning($"SupermatterLightningEvent.Activate: Invalid next event range: min={comp.LightningMinNextEvent}, max={comp.LightningMaxNextEvent}");
            return;
        }

        if (comp.LightningMinNextEvent > comp.LightningMaxNextEvent)
        {
            system.Log.Warning($"SupermatterLightningEvent.Activate: Min next event time greater than max: min={comp.LightningMinNextEvent}, max={comp.LightningMaxNextEvent}");
            return;
        }

        if (comp.LightningSpawnDuration <= 0)
        {
            system.Log.Warning($"SupermatterLightningEvent.Activate: Invalid LightningSpawnDuration: {comp.LightningSpawnDuration}");
            return;
        }

        if (comp.LightningCooldownDuration <= 0)
        {
            system.Log.Warning($"SupermatterLightningEvent.Activate: Invalid LightningCooldownDuration: {comp.LightningCooldownDuration}");
            return;
        }

        try
        {
            var currentTime = system.GameTiming.CurTime;
            comp.CurrentEvent = SupermatterEventType.Lightning;
            comp.EventEndTime = TimeSpan.FromSeconds(comp.LightningEventDuration);
            comp.NextEventTimer = TimeSpan.FromSeconds(system.Random.NextFloat(comp.LightningMinNextEvent, comp.LightningMaxNextEvent));
            comp.LightningCooldown = TimeSpan.Zero;
            comp.LastEventEndTimeUpdate = currentTime;
            comp.LastNextEventTimerUpdate = currentTime;
            comp.LastLightningCooldownUpdate = currentTime;

            // Обработка потенциальных исключений при создании молнии
            system.ImperialLightning?.SpawnLightningBetween(uid, uid, null, null, TimeSpan.FromSeconds(comp.LightningSpawnDuration));
        }
        catch (Exception ex)
        {
            system.Log.Error($"SupermatterLightningEvent.Activate: Exception during activation for entity {uid}: {ex.Message}");
        }
    }

    public void Process(EntityUid uid, SupermatterEventComponent comp, SupermatterEventSystem system, TimeSpan currentTime)
    {
        var elapsedSinceLastUpdate = currentTime - comp.LastLightningCooldownUpdate;
        comp.LightningCooldown -= elapsedSinceLastUpdate;
        comp.LastLightningCooldownUpdate = currentTime;

        if (comp.LightningCooldown <= TimeSpan.Zero)
        {
            system.ImperialLightning?.SpawnLightningBetween(uid, uid, null, null, TimeSpan.FromSeconds(comp.LightningSpawnDuration));

            if (system.TryGetComponent<SupermatterIntegrityComponent>(uid, out var integrity) && integrity != null &&
                system.TryGetComponent<DamageableComponent>(uid, out var _))
            {
                system.Damageable.TryChangeDamage(uid, integrity.TickDamage, false, true, origin: null);
            }

            comp.LightningCooldown = TimeSpan.FromSeconds(comp.LightningCooldownDuration);
        }
    }

    public string GetAnnouncement()
    {
        return Loc.GetString("supermatter-event-lightning");
    }
}
