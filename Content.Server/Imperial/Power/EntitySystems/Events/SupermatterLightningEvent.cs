using Content.Server.Imperial.Power.Components;
using Content.Shared.Damage;
using Robust.Shared.Localization;
using System;

namespace Content.Server.Imperial.Power.EntitySystems.Events;

/// <summary>
/// Событие "Молния" - суперматерия генерирует электрические разряды
/// </summary>
public sealed class SupermatterLightningEvent
{
    public static void Activate(EntityUid uid, SupermatterEventComponent comp, SupermatterEventSystem supermatterSystem)
    {
        // Валидация входных параметров
        if (uid == EntityUid.Invalid)
        {
            supermatterSystem.Log.Error("SupermatterLightningEvent.Activate: Invalid EntityUid provided");
            return;
        }
        if (comp == null)
        {
            return;
        }

        if (supermatterSystem == null)
        {
            return;
        }

        // Валидация конфигурации компонента
        if (comp.LightningEventDuration <= TimeSpan.Zero)
        {
            supermatterSystem.Log.Warning($"SupermatterLightningEvent.Activate: Invalid LightningEventDuration: {comp.LightningEventDuration}");
            return;
        }

        if (comp.LightningCooldownDuration <= TimeSpan.Zero)
        {
            supermatterSystem.Log.Warning($"SupermatterLightningEvent.Activate: Invalid LightningCooldownDuration: {comp.LightningCooldownDuration}");
            return;
        }

        var currentTime = supermatterSystem.GameTiming.CurTime;
        comp.CurrentEvent = SupermatterEventComponent.SupermatterEventType.Lightning;
        comp.EventEndTime = comp.LightningEventDuration;
        comp.NextEventTimer = comp.EventAfterLightingTime;
        comp.LightningCooldown = TimeSpan.Zero;
        comp.LastEventEndTimeUpdate = currentTime;
        comp.LastNextEventTimerUpdate = currentTime;
        comp.LastLightningCooldownUpdate = currentTime;

        // Стреляем молнии в случайные цели вокруг суперматерии
        ShootRandomLightnings(uid, supermatterSystem, comp);
    }

    public static void Process(EntityUid uid, SupermatterEventComponent comp, SupermatterEventSystem supermatterSystem, TimeSpan currentTime)
    {
        var elapsedSinceLastUpdate = currentTime - comp.LastLightningCooldownUpdate;
        comp.LightningCooldown -= elapsedSinceLastUpdate;
        comp.LastLightningCooldownUpdate = currentTime;

        if (comp.LightningCooldown > TimeSpan.Zero)
            return;

        // Стреляем молнии в случайные цели вокруг суперматерии
        ShootRandomLightnings(uid, supermatterSystem, comp);

        if (supermatterSystem.TryGetComponent<SupermatterIntegrityComponent>(uid, out var integrity) && integrity != null &&
            supermatterSystem.TryGetComponent<DamageableComponent>(uid, out var _))
        {
            supermatterSystem.Damageable.TryChangeDamage(uid, integrity.TickDamage, origin: null);
        }

        comp.LightningCooldown = comp.LightningCooldownDuration;
    }

    private static void ShootRandomLightnings(EntityUid uid, SupermatterEventSystem supermatterSystem, SupermatterEventComponent component)
    {
        // Используем ShootRandomLightnings для стрельбы в случайные цели в радиусе
        supermatterSystem.LightningSystem?.ShootRandomLightnings(uid, component.LightningBoltRadius, component.LightningBoltCount, "Lightning", 0, true);
    }

    public static string GetAnnouncement()
    {
        return Loc.GetString("supermatter-event-lightning");
    }
}

