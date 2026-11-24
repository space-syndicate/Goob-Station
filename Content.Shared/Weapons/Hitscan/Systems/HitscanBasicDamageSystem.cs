using Content.Shared.Administration.Logs; // CorvaxGoob-LogHitPart
using Content.Shared.Damage.Systems;
using Content.Shared.Database; // CorvaxGoob-LogHitPart
using Content.Shared.Weapons.Hitscan.Components;
using Content.Shared.Weapons.Hitscan.Events;

namespace Content.Shared.Weapons.Hitscan.Systems;

public sealed class HitscanBasicDamageSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private ISharedAdminLogManager _log = default!; // CorvaxGoob-LogHitPart

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HitscanBasicDamageComponent, HitscanRaycastFiredEvent>(OnHitscanHit);
    }

    private void OnHitscanHit(Entity<HitscanBasicDamageComponent> ent, ref HitscanRaycastFiredEvent args)
    {
        if (args.Data.HitEntity == null)
            return;

        var dmg = ent.Comp.Damage * _damage.UniversalHitscanDamageModifier;

        // CorvaxGoob-LogHitPart-start
        //if(!_damage.TryChangeDamage(args.Data.HitEntity.Value, dmg, out var damageDealt, origin: args.Data.Gun))
        //    return;
        var damaged = _damage.TryChangeDamage(args.Data.HitEntity.Value, dmg, out var damageDealt, origin: args.Data.Gun);

        _log.Add(LogType.HitScanHit,
            $"{ToPrettyString(args.Data.Shooter ?? args.Data.Gun):user} hit {ToPrettyString(args.Data.HitEntity.Value):target}"
            + $" using {ToPrettyString(args.Data.Gun):entity}"
            + $" and dealt {damageDealt.GetTotal():damage} damage to {(damageDealt.HitPart?.ToString() ?? "none"):part}");

        if (!damaged)
            return;
        // CorvaxGoob-LogHitPart-end

        var damageEvent = new HitscanDamageDealtEvent
        {
            Target = args.Data.HitEntity.Value,
            DamageDealt = damageDealt,
        };

        RaiseLocalEvent(ent, ref damageEvent);
    }
}
