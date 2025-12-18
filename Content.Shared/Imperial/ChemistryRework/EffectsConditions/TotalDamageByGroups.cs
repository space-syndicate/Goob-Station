using System.Linq;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityConditions;
using Content.Shared.FixedPoint;
using Content.Shared.Localizations;
using Robust.Shared.Prototypes;

namespace Content.Shared.Chemistry.ReagentEffectConditions;


public sealed partial class TotalDamageByGroupsEntityConditionSystem : EntityConditionSystem<DamageableComponent, TotalDamageByGroups>
{
    protected override void Condition(Entity<DamageableComponent> entity, ref EntityConditionEvent<TotalDamageByGroups> args)
    {
        var totalDamage = new FixedPoint2();

        args.Condition.DamageTypes.ToList().ForEach(type => totalDamage += entity.Comp.Damage[type]);
        args.Condition.DamageGroups.ToList().ForEach(group => totalDamage += entity.Comp.DamagePerGroup[group]);

        args.Result = totalDamage > args.Condition.Min && totalDamage < args.Condition.Max;
    }
}

public sealed partial class TotalDamageByGroups : EntityConditionBase<TotalDamageByGroups>
{
    [DataField]
    public FixedPoint2 Max = FixedPoint2.MaxValue;

    [DataField]
    public FixedPoint2 Min = FixedPoint2.Zero;

    [DataField]
    public List<ProtoId<DamageTypePrototype>> DamageTypes = new();

    [DataField]
    public List<ProtoId<DamageGroupPrototype>> DamageGroups = new();

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
    {
        return Loc.GetString("reagent-effect-condition-guidebook-total-by-groups-damage",
            ("max", Max == FixedPoint2.MaxValue ? int.MaxValue : Max.Float()),
            ("min", Min.Float()),
            ("groups", ContentLocalizationManager.FormatList(DamageGroups.Select(el => el.Id).ToList())),
            ("types", ContentLocalizationManager.FormatList(DamageTypes.Select(el => el.Id).ToList()))
        );
    }
}
