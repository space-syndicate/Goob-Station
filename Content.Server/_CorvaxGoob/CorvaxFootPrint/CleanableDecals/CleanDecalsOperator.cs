using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Shared.Timing;

namespace Content.Server._CorvaxGoob.CorvaxFootPrint.CleanableDecals;

/// <summary>
///     Очищает все очищаемые декали в радиусе.
///     Берёт декали из хэша <see cref="CleanableDecalTrackerSystem"/>
/// </summary>
public sealed partial class CleanDecalsOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    private SharedTransformSystem _transform = default!;
    private CleanableDecalTrackerSystem _tracker = default!;
    private UseDelaySystem _useDelay = default!;

    /// <summary>
    ///     Радиус очистки декалей вокруг бота.
    /// </summary>
    [DataField]
    public float CleanRadius = 1.5f;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _transform = sysManager.GetEntitySystem<SharedTransformSystem>();
        _tracker = sysManager.GetEntitySystem<CleanableDecalTrackerSystem>();
        _useDelay = sysManager.GetEntitySystem<UseDelaySystem>();
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        // Задержка использования, чтобы бот не чистил каждый тик
        _entManager.TryGetComponent<UseDelayComponent>(owner, out var useDelay);
        if (useDelay != null && _useDelay.IsDelayed((owner, useDelay)))
            return HTNOperatorStatus.Continuing;

        if (!_entManager.TryGetComponent<TransformComponent>(owner, out var xform)
            || xform.GridUid is not { } grid)
            return HTNOperatorStatus.Failed;

        var localPos = _transform.ToCoordinates(grid, _transform.GetMapCoordinates(owner, xform)).Position;

        if (_tracker.CleanInRange(grid, localPos, CleanRadius) > 0 && useDelay != null)
            _useDelay.TryResetDelay((owner, useDelay));

        return HTNOperatorStatus.Finished;
    }
}
