using Content.Shared.Alert;
using Content.Shared.Damage.Systems;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Robust.Shared.Timing;

namespace Content.Shared.Imperial.XxRaay.Zero.Overthrow;

public abstract class SharedOverthrowSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;

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
        var currentTime = _gameTiming.CurTime;

        if (currentTime < component.LastThrowTime + component.Cooldown)
        {
            var cooldownComp = EnsureComp<OverthrownItemComponent>(ev.ItemUid);
            cooldownComp.Damage = component.Damage;
            cooldownComp.KnockdownTime = component.KnockdownTime;
            cooldownComp.IsOverthrown = false;
            Dirty(ev.ItemUid, cooldownComp);
            return;
        }

        ev.ThrowSpeed *= component.ThrowMultiplier;

        var overthrownComp = EnsureComp<OverthrownItemComponent>(ev.ItemUid);
        overthrownComp.Damage = component.Damage;
        overthrownComp.KnockdownTime = component.KnockdownTime;
        overthrownComp.IsOverthrown = true;
        Dirty(ev.ItemUid, overthrownComp);

        component.LastThrowTime = currentTime;
        Dirty(entity);

        var cooldownEnd = currentTime + component.Cooldown;
        _alerts.ShowAlert(entity.Owner, component.CooldownAlertId, cooldown: (currentTime, cooldownEnd), autoRemove: true);

        ScheduleUpdateAlert(entity, cooldownEnd);
    }

    private void OnOverthrowStartup(Entity<OverthrowComponent> entity, ref ComponentStartup args)
    {
        UpdateOverthrowAlert(entity);
    }

    private void UpdateOverthrowAlert(Entity<OverthrowComponent> entity)
    {
        var component = entity.Comp;
        var currentTime = _gameTiming.CurTime;

        if (currentTime < component.LastThrowTime + component.Cooldown)
        {
            var cooldownEnd = component.LastThrowTime + component.Cooldown;
            _alerts.ShowAlert(entity.Owner, component.CooldownAlertId, cooldown: (component.LastThrowTime, cooldownEnd), autoRemove: true);
        }
        else
        {
            _alerts.ShowAlert(entity.Owner, component.CooldownAlertId);
        }
    }

    private void ScheduleUpdateAlert(Entity<OverthrowComponent> entity, TimeSpan cooldownEnd)
    {
        var delay = cooldownEnd - _gameTiming.CurTime;
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

        _damageable.TryChangeDamage(ev.Target, component.Damage);
        _stun.TryKnockdown(ev.Target, component.KnockdownTime);

        RemComp<OverthrownItemComponent>(entity);
    }
}

