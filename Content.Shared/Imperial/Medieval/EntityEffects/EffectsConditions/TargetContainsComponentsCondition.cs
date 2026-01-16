using Content.Shared.EntityConditions;
using Robust.Shared.Prototypes;

namespace Content.Server.EntityEffects.EffectConditions;


public sealed partial class TargetContainsComponentsEntityConditionSystem : EntityConditionSystem<MetaDataComponent, TargetContainsComponentsCondition>
{
    [Dependency] private readonly IEntityManager _entityManager = default!;


    protected override void Condition(Entity<MetaDataComponent> entity, ref EntityConditionEvent<TargetContainsComponentsCondition> args)
    {
        args.Result = CheckBlacklist(entity, args) && CheckWhitelist(entity, args);
    }

    #region Helpers

    private bool CheckWhitelist(EntityUid target, EntityConditionEvent<TargetContainsComponentsCondition> args)
    {
        foreach (var (_, componentRegistry) in args.Condition.ComponentsWhitelist)
        {
            if (_entityManager.HasComponent(target, componentRegistry.Component.GetType())) continue;

            return false;
        }

        return true;
    }

    private bool CheckBlacklist(EntityUid target, EntityConditionEvent<TargetContainsComponentsCondition> args)
    {
        foreach (var (_, componentRegistry) in args.Condition.ComponentsBlacklist)
        {
            if (!_entityManager.HasComponent(target, componentRegistry.Component.GetType())) continue;

            return false;
        }

        return true;
    }

    #endregion
}



/// <summary>
/// Proves whether the target has protective components
/// </summary>
public sealed partial class TargetContainsComponentsCondition : EntityConditionBase<TargetContainsComponentsCondition>
{
    /// <summary>
    /// Components for check
    /// </summary>
    [DataField]
    public ComponentRegistry ComponentsWhitelist = new();

    /// <summary>
    /// Components for check
    /// </summary>
    [DataField]
    public ComponentRegistry ComponentsBlacklist = new();


    public override string EntityConditionGuidebookText(IPrototypeManager prototype) => "";
}
