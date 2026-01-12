using Content.Server.Imperial.Vampire;
using Content.Server.Objectives.Systems;
using Content.Shared.Imperial.Vampire;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;

namespace Content.Server.Imperial.Vampire;

public sealed class VampirePurposesSystem : EntitySystem
{
    [Dependency] private readonly NumberObjectiveSystem _number = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VampireDrinkBloodPurposesComponent, ObjectiveGetProgressEvent>(OnDrinkBloodProgress);
        SubscribeLocalEvent<VampireConvertedGhoulsPurposesComponent, ObjectiveGetProgressEvent>(OnConvertedGhoulsProgress);
    }

    private void OnDrinkBloodProgress(EntityUid uid, VampireDrinkBloodPurposesComponent comp, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = GetDrinkBloodProgress(args.MindId, _number.GetTarget(uid));
    }

    private void OnConvertedGhoulsProgress(EntityUid uid, VampireConvertedGhoulsPurposesComponent comp, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = GetConvertedGhoulsProgress(args.MindId, _number.GetTarget(uid));
    }

    private float GetDrinkBloodProgress(EntityUid mindId, int target)
    {
        if (!TryComp<MindComponent>(mindId, out var mindComp))
            return 0f;

        if (mindComp.OwnedEntity == null)
            return 0f;

        if (!TryComp<VampireComponent>(mindComp.OwnedEntity.Value, out var vamp))
            return 0f;

        if (target == 0)
            return 1f;

        return MathF.Min(vamp.TotalDrunk / target, 1f);
    }

    private float GetConvertedGhoulsProgress(EntityUid mindId, int target)
    {
        if (!TryComp<MindComponent>(mindId, out var mindComp))
            return 0f;

        if (mindComp.OwnedEntity == null)
            return 0f;

        if (!TryComp<VampireComponent>(mindComp.OwnedEntity.Value, out var vamp))
            return 0f;

        if (target == 0)
            return 1f;

        return MathF.Min(vamp.GhoulQuantity / target, 1f);
    }
}
