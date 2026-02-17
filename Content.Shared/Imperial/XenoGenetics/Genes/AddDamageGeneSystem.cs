using Content.Shared.Imperial.XenoGenetics;
using Robust.Shared.Prototypes;
using System.Linq;
using Content.Shared.Imperial.XenoGenetics.Genes.Components;
using Content.Shared.Imperial.XenoGenetics.Components;
using Microsoft.VisualBasic;
using Content.Shared.Damage;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Shared.Imperial.XenoGenetics.Genes;

public sealed class AddDamageGeneSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageIncreasedByGeneComponent, GetMeleeDamageEvent>(OnGetMeleeDamage); 
        SubscribeLocalEvent<DamageIncreasedByGeneComponent, GetMeleeAttackRateEvent>(OnGetAttackRate);
    }
    private void OnGetMeleeDamage(EntityUid uid, DamageIncreasedByGeneComponent component, ref GetMeleeDamageEvent args)
    {
        if(component.BonusDamage == null)
            return;
        args.Damage += component.BonusDamage;
    }
    private void OnGetAttackRate(EntityUid uid, DamageIncreasedByGeneComponent component, ref GetMeleeAttackRateEvent args)
    {
        if(component.BonusAttackRate == null)
            return;
        args.Rate += component.BonusAttackRate;
    }

}