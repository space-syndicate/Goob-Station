using System.Threading;
using System.Threading.Tasks;
using Content.Server.NPC;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Robust.Shared.Map;

namespace Content.Server._CorvaxGoob.CorvaxFootPrint.CleanableDecals;

/// <summary>
///     Находит ближайшую очищаемую декаль в радиусе видимости и кладёт её координаты
///     в <see cref="NPCBlackboard"/>, чтобы MoveToOperator довёл бота до неё. Использует хэш
///     <see cref="CleanableDecalTrackerSystem"/> вместо перебора всех декалей.
/// </summary>
public sealed partial class PickCleanableDecalOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    private SharedTransformSystem _transform = default!;
    private CleanableDecalTrackerSystem _tracker = default!;

    /// <summary>
    ///     Куда записать координаты найденной декали.
    /// </summary>
    [DataField]
    public string TargetCoordinatesKey = "CleanDecalCoordinates";

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _transform = sysManager.GetEntitySystem<SharedTransformSystem>();
        _tracker = sysManager.GetEntitySystem<CleanableDecalTrackerSystem>();
    }

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(
        NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!_entManager.TryGetComponent<TransformComponent>(owner, out var xform)
            || xform.GridUid is not { } grid)
            return (false, null);

        var defaultRange = 10f; // стандартный радиус видимости если GetVisionRadiusKey = 0

        var range = blackboard.GetValueOrDefault<float>(blackboard.GetVisionRadiusKey(_entManager), _entManager);
        if (range <= 0f)
            range = defaultRange;

        var localPos = _transform.ToCoordinates(grid, _transform.GetMapCoordinates(owner, xform)).Position;

        if (!_tracker.TryFindNearest(grid, localPos, range, out var target, out _))
            return (false, null);

        return (true, new Dictionary<string, object>
        {
            { TargetCoordinatesKey, new EntityCoordinates(grid, target) },
        });
    }
}
