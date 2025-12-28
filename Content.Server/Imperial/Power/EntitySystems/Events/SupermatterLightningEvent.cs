using Content.Server.Imperial.Power.Components;
using Content.Shared.Damage.Components;

namespace Content.Server.Imperial.Power.EntitySystems.Events;

/// <summary>
/// Событие "Молния" - суперматерия генерирует электрические разряды
/// </summary>
public sealed class SupermatterLightningEvent
{
    public static void Activate(Entity<SupermatterEventComponent> entity, SupermatterEventSystem supermatterSystem)
    {
        // Валидация входных параметров
        if (entity.AsType() == EntityUid.Invalid)
        {
            supermatterSystem.Log.Error("SupermatterLightningEvent.Activate: Invalid EntityUid provided");
            return;
        }

        // Валидация конфигурации компонента
        if (entity.Comp.LightningEventDuration <= TimeSpan.Zero)
        {
            supermatterSystem.Log.Warning($"SupermatterLightningEvent.Activate: Invalid LightningEventDuration: {entity.Comp.LightningEventDuration}");
            return;
        }

        if (entity.Comp.LightningCooldownDuration <= TimeSpan.Zero)
        {
            supermatterSystem.Log.Warning($"SupermatterLightningEvent.Activate: Invalid LightningCooldownDuration: {entity.Comp.LightningCooldownDuration}");
            return;
        }

        var currentTime = supermatterSystem.GameTiming.CurTime;
        entity.Comp.CurrentEvent = SupermatterEventComponent.SupermatterEventType.Lightning;
        entity.Comp.EventEndTime = entity.Comp.LightningEventDuration;
        entity.Comp.NextEventTimer = entity.Comp.EventAfterLightingTime;
        entity.Comp.LightningCooldown = TimeSpan.Zero;
        entity.Comp.LastEventEndTimeUpdate = currentTime;
        entity.Comp.LastNextEventTimerUpdate = currentTime;
        entity.Comp.LastLightningCooldownUpdate = currentTime;

        // Стреляем молнии в случайные цели вокруг суперматерии
        ShootRandomLightnings(entity, supermatterSystem, entity.Comp);
    }

    public static void Process(Entity<SupermatterEventComponent> entity, SupermatterEventSystem supermatterSystem, TimeSpan currentTime)
    {
        var elapsedSinceLastUpdate = currentTime - entity.Comp.LastLightningCooldownUpdate;
        entity.Comp.LightningCooldown -= elapsedSinceLastUpdate;
        entity.Comp.LastLightningCooldownUpdate = currentTime;

        if (entity.Comp.LightningCooldown > TimeSpan.Zero)
            return;

        // Стреляем молнии в случайные цели вокруг суперматерии
        ShootRandomLightnings(entity, supermatterSystem, entity.Comp);

        if (supermatterSystem.TryGetComponent<SupermatterIntegrityComponent>(entity, out var integrity) && integrity != null &&
            supermatterSystem.TryGetComponent<DamageableComponent>(entity, out _))
        {
            supermatterSystem.Damageable.TryChangeDamage(entity.Owner, integrity.TickDamage, origin: null);
        }

        entity.Comp.LightningCooldown = entity.Comp.LightningCooldownDuration;
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

