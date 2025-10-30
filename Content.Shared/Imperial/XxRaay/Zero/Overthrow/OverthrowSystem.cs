using Content.Shared.Alert;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Robust.Shared.Timing;

namespace Content.Shared.Imperial.XxRaay.Zero.Overthrow;

public abstract class OverthrowSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming GameTiming = default!;
    [Dependency] protected readonly AlertsSystem Alerts = default!;
    [Dependency] protected readonly DamageableSystem Damageable = default!;
    [Dependency] protected readonly SharedStunSystem Stun = default!;


    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeLocalEvent<OverthrowComponent, BeforeThrowEvent>(OnBeforeThrow);
        SubscribeLocalEvent<OverthrowComponent, ComponentStartup>(OnOverthrowStartup);
        SubscribeLocalEvent<OverthrownItemComponent, ThrowDoHitEvent>(OnThrowHit);
    }

    private void OnBeforeThrow(Entity<OverthrowComponent> entity, ref BeforeThrowEvent ev)
    {
        if (ev.Cancelled)
            return;

        var component = entity.Comp;
        var currentTime = GameTiming.CurTime;
        
        if (component.LastThrowTime.HasValue && 
            currentTime < component.LastThrowTime.Value + TimeSpan.FromSeconds(component.Cooldown))
        {
            if (TryComp<OverthrownItemComponent>(ev.ItemUid, out var cooldownComp))
            {
                cooldownComp.IsOverthrown = false;
                Dirty(ev.ItemUid, cooldownComp);
            }
            else
            {
                var overthrownComp = new OverthrownItemComponent
                {
                    Damage = component.Damage,
                    KnockdownTime = component.KnockdownTime,
                    IsOverthrown = false
                };
                AddComp(ev.ItemUid, overthrownComp);
            }
            return;
        }

        ev.ThrowSpeed *= component.ThrowMultiplier;
        
        if (TryComp<OverthrownItemComponent>(ev.ItemUid, out var existingComp))
        {
            existingComp.Damage = component.Damage;
            existingComp.KnockdownTime = component.KnockdownTime;
            existingComp.IsOverthrown = true;
            Dirty(ev.ItemUid, existingComp);
        }
        else
        {
            var overthrownComp = new OverthrownItemComponent
            {
                Damage = component.Damage,
                KnockdownTime = component.KnockdownTime,
                IsOverthrown = true
            };
            AddComp(ev.ItemUid, overthrownComp);
        }
        
        component.LastThrowTime = currentTime;
        Dirty(entity);
        
        var cooldownEnd = currentTime + TimeSpan.FromSeconds(component.Cooldown);
        Alerts.ShowAlert(entity.Owner, "OverthrowCooldown", cooldown: (currentTime, cooldownEnd), autoRemove: true);
        
        
        ScheduleUpdateAlert(entity, cooldownEnd);
    }

    private void OnOverthrowStartup(Entity<OverthrowComponent> entity, ref ComponentStartup args)
    {
        UpdateOverthrowAlert(entity);
    }

    private void UpdateOverthrowAlert(Entity<OverthrowComponent> entity)
    {
        var component = entity.Comp;
        var currentTime = GameTiming.CurTime;
        
        if (component.LastThrowTime.HasValue && 
            currentTime < component.LastThrowTime.Value + TimeSpan.FromSeconds(component.Cooldown))
        {
            var cooldownEnd = component.LastThrowTime.Value + TimeSpan.FromSeconds(component.Cooldown);
            Alerts.ShowAlert(entity.Owner, "OverthrowCooldown", cooldown: (component.LastThrowTime.Value, cooldownEnd), autoRemove: true);
        }
        else
        {
            Alerts.ShowAlert(entity.Owner, "OverthrowCooldown");
        }
    }

    private void ScheduleUpdateAlert(Entity<OverthrowComponent> entity, TimeSpan cooldownEnd)
    {
        var delay = cooldownEnd - GameTiming.CurTime;
        if (delay > TimeSpan.Zero)
        {
            Timer.Spawn(delay, () =>
            {
                if (Exists(entity) && HasComp<OverthrowComponent>(entity))
                {
                    UpdateOverthrowAlert(entity);
                }
            });
        }
    }

    private void OnThrowHit(Entity<OverthrownItemComponent> entity, ref ThrowDoHitEvent ev)
    {
        var component = entity.Comp;
        
        if (!component.IsOverthrown)
        {
            RemComp<OverthrownItemComponent>(entity);
            return;
        }
        
        var damageSpec = new DamageSpecifier();
        damageSpec.DamageDict.Add("Blunt", FixedPoint2.New(component.Damage));
        
        Damageable.TryChangeDamage(ev.Target, damageSpec);
        Stun.TryKnockdown(ev.Target, TimeSpan.FromSeconds(component.KnockdownTime));
        
        RemComp<OverthrownItemComponent>(entity);
    }
}
