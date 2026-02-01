using Content.Shared.EntityConditions;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.EffectConditions;

public sealed partial class MagicTargetMobStateEntityConditionSystem : EntityConditionSystem<MobStateComponent, MagicTargetMobStateCondition>
{
    protected override void Condition(Entity<MobStateComponent> entity, ref EntityConditionEvent<MagicTargetMobStateCondition> args)
    {
        args.Result = args.Condition.AllowedState == entity.Comp.CurrentState;
    }
}


/// <summary>
/// Checks target state
/// </summary>
public sealed partial class MagicTargetMobStateCondition : EntityConditionBase<MagicTargetMobStateCondition>
{
    [DataField(required: true)]
    public MobState AllowedState = MobState.Alive;


    public override string EntityConditionGuidebookText(IPrototypeManager prototype) => "";
}
