using Content.Shared.Imperial.XenoGenetics;
using Robust.Shared.Prototypes;
using System.Linq;
using Content.Shared.Imperial.XenoGenetics.Genes.Components;
using Content.Shared.Imperial.XenoGenetics.Components;
using Microsoft.VisualBasic;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;

namespace Content.Shared.Imperial.XenoGenetics.Genes;

public sealed class AddArmorGeneSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AddDamageResistanceGeneComponent, DamageModifyEvent>(OnDamageModify);
    }
    private void OnDamageModify(EntityUid uid, AddDamageResistanceGeneComponent component, ref DamageModifyEvent args)
    {
        args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, component.Modifiers);
    }
}