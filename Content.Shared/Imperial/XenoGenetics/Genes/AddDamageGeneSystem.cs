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

        //SubscribeLocalEvent<DamageIncreaserGeneComponent, GeneInsertedEvent>(OnGeneInsert);
        //SubscribeLocalEvent<DamageIncreaserGeneComponent, GeneWithdrawnEvent>(OnGeneWithdraw);

        SubscribeLocalEvent<DamageIncreasedByGeneComponent, GetMeleeDamageEvent>(OnGetMeleeDamage); 
        SubscribeLocalEvent<DamageIncreasedByGeneComponent, GetMeleeAttackRateEvent>(OnGetAttackRate);
    }
    // пока что не придумал как это сделать
    //private void OnGeneInsert(EntityUid uid, DamageIncreaserGeneComponent component, ref GeneInsertedEvent args)
    //{
//        var geneComp = EnsureComp<XenoGeneComponent>(args.Gene);
//        EntityManager.AddComponents(args.Target, component.DamageGene, false);
//        var damageGene = EnsureComp<DamageIncreasedByGeneComponent>(args.Gene);
//        if (component.RandomStats == true)
//        {
//            foreach (var stat in damageGene.BonusDamage.DamageDict) 
//            {
//                stat.Value = stat.Value;
//            }
//        }
//    }
//    private void OnGeneWithdraw(EntityUid uid, DamageIncreaserGeneComponent component, ref GeneWithdrawnEvent args)
//    {
//        EntityManager.RemoveComponents(args.Target, component.DamageGene);
//    }
    private void OnGetMeleeDamage(EntityUid uid, DamageIncreasedByGeneComponent component, ref GetMeleeDamageEvent args)
    {
        if (component.BonusDamage == null)
            return;
        args.Damage += component.BonusDamage;
    }
    private void OnGetAttackRate(EntityUid uid, DamageIncreasedByGeneComponent component, ref GetMeleeAttackRateEvent args)
    {
        args.Rate += component.BonusAttackRate;
    }
}