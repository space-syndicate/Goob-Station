using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Imperial.Damage;
using Content.Shared.Whitelist;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Server.Imperial.Damage;

public sealed class ImperialDamageOnCollideSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ImperialDamageOnCollideComponent, StartCollideEvent>(OnCollideStart);
        SubscribeLocalEvent<ImperialDamageOnCollideComponent, EndCollideEvent>(OnCollideEnd);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ImperialDamagedByCollideComponent>();
        while (query.MoveNext(out var ent, out var damaged))
        {
            if (!damaged.TimeDamage.HasValue)
                continue;

            if (!damaged.TimeDamage.Value)
            {
                if (damaged.DamageOnce && damaged.Damage != null)
                {
                    _damageable.TryChangeDamage(ent, damaged.Damage, interruptsDoAfters: false);
                    damaged.DamageOnce = false;
                }

                DelCompDamagedByCollide(ent);

                continue;
            }

            if (damaged.NextTimeDamage == null || damaged.EndTimeDamage == null)
            {
                DelCompDamagedByCollide(ent);

                continue;
            }

            if (damaged.AddTimeEndDamage)
            {
                damaged.EndSecond = _timing.CurTime + damaged.EndTimeDamage.Value;
                damaged.AddTimeEndDamage = false;
                damaged.NextSecond = _timing.CurTime;

                DelCompDamagedByCollide(ent);
            }

            if (_timing.CurTime >= damaged.EndSecond)
            {
                DelCompDamagedByCollide(ent);

                continue;
            }

            if (_timing.CurTime < damaged.NextSecond)
                continue;

            damaged.NextSecond = _timing.CurTime + damaged.NextTimeDamage.Value;

            if (damaged.Damage != null)
                _damageable.TryChangeDamage(ent, damaged.Damage, interruptsDoAfters: false);
        }
    }

    private void OnCollideStart(EntityUid uid, ImperialDamageOnCollideComponent component, ref StartCollideEvent args)
    {
        var otherUid = args.OtherEntity;
        if (HasComp<ImperialDamagedByCollideComponent>(otherUid))
            return;

        if (_whitelistSystem.IsWhitelistPass(component.IgnoreWhitelist, otherUid))
            return;

        var damagedByCollide = EnsureComp<ImperialDamagedByCollideComponent>(otherUid);
        damagedByCollide.Damage = component.Damage;
        damagedByCollide.TimeDamage = component.TimeDamage;
        damagedByCollide.NextTimeDamage = component.NextTimeDamage;
        damagedByCollide.EndTimeDamage = component.EndTimeDamage;
    }

    private void OnCollideEnd(EntityUid uid, ImperialDamageOnCollideComponent component, ref EndCollideEvent args)
    {
        var otherUid = args.OtherEntity;
        if (!TryComp<PhysicsComponent>(otherUid, out var body))
            return;

        var damageQuery = GetEntityQuery<ImperialDamageOnCollideComponent>();
        foreach (var ent in _physics.GetContactingEntities(otherUid, body))
        {
            if (ent == uid)
                continue;

            if (damageQuery.HasComponent(ent))
                return;
        }

        DelCompDamageOnCollide(otherUid);
    }

    private void DelCompDamageOnCollide(EntityUid uid)
    {
        if (HasComp<ImperialDamageOnCollideComponent>(uid))
            RemComp<ImperialDamageOnCollideComponent>(uid);
    }

    private void DelCompDamagedByCollide(EntityUid uid)
    {
        if (HasComp<ImperialDamagedByCollideComponent>(uid))
            RemComp<ImperialDamagedByCollideComponent>(uid);
    }
}
