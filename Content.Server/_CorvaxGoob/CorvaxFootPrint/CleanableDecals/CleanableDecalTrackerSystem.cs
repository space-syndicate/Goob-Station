using System.Numerics;
using Content.Server.Decals;
using Content.Shared.Decals;
using Content.Shared.GameTicking;
using Robust.Shared.Utility;

namespace Content.Server._CorvaxGoob.CorvaxFootPrint.CleanableDecals;

/// <summary>
///     Держит хэш очищаемых декалей, чтобы избежать постоянного вызова <see cref="DecalSystem.GetDecalsInRange"/>
///     Без содержания декали ва хэше NPC не станет убирать эту декаль.
///     Рантайм-декали ловятся через <see cref="DecalAddedEvent"/>/<see cref="DecalRemovedEvent"/>
/// </summary>
public sealed class CleanableDecalTrackerSystem : EntitySystem
{
    [Dependency] private readonly DecalSystem _decals = default!;
    private readonly Dictionary<EntityUid, Dictionary<Vector2i, Dictionary<uint, Vector2>>> _tracked = new();

    // Гриды, для которых уже выполнен разовый сид декалей с карты.
    private readonly HashSet<EntityUid> _seeded = new();

    //пПереиспользуемые буферы, чтобы не аллоцировать при уборке
    private readonly List<uint> _stale = new();
    private readonly List<uint> _cleanBuffer = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DecalGridComponent, ComponentShutdown>(OnDecalGridShutdown);
        SubscribeLocalEvent<DecalGridComponent, DecalAddedEvent>(OnDecalAdded);
        SubscribeLocalEvent<DecalGridComponent, DecalRemovedEvent>(OnDecalRemoved);

        // на всякий случай: после конца раунда гриды удаляются, но это для спокойствия.
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnDecalGridShutdown(EntityUid uid, DecalGridComponent component, ComponentShutdown args)
    {
        _tracked.Remove(uid);
        _seeded.Remove(uid);
    }

    /// <summary>
    ///     минуем подпись на ComponentStartup
    /// </summary>
    private void EnsureSeeded(EntityUid grid, DecalGridComponent decalGrid)
    {
        if (!_seeded.Add(grid))
            return;

        foreach (var (chunkIndex, chunk) in decalGrid.ChunkCollection.ChunkCollection)
        {
            foreach (var (id, decal) in chunk.Decals)
            {
                if (decal.Cleanable)
                    _tracked.GetOrNew(grid).GetOrNew(chunkIndex)[id] = decal.Coordinates;
            }
        }
    }

    private void OnDecalAdded(EntityUid uid, DecalGridComponent component, ref DecalAddedEvent args)
    {
        if (!args.Decal.Cleanable)
            return;

        Register(uid, args.DecalId, args.Decal.Coordinates);
    }

    private void OnDecalRemoved(EntityUid uid, DecalGridComponent component, ref DecalRemovedEvent args)
    {
        UnregisterByChunk(uid, args.DecalId, args.ChunkIndices);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent args)
    {
        _tracked.Clear();
        _seeded.Clear();
    }

    private void Register(EntityUid grid, uint decalId, Vector2 coordinates)
    {
        var chunk = SharedDecalSystem.GetChunkIndices(coordinates);
        _tracked.GetOrNew(grid).GetOrNew(chunk)[decalId] = coordinates;
    }

    private void UnregisterByChunk(EntityUid grid, uint decalId, Vector2i chunk)
    {
        if (!_tracked.TryGetValue(grid, out var chunks) || !chunks.TryGetValue(chunk, out var decals))
            return;

        decals.Remove(decalId);

        if (decals.Count == 0)
            chunks.Remove(chunk);
        if (chunks.Count == 0)
            _tracked.Remove(grid);
    }

    /// <summary>
    ///     Находит ближайшую к <paramref name="localPos"/> очищаемую декаль в пределах <paramref name="range"/>
    ///     на указанном гриде, читая только хэш.
    /// </summary>
    /// <param name="localPos">Позиция бота в координатах грида.</param>
    /// <param name="target">Центр найденной декали, или же цель движения.</param>
    public bool TryFindNearest(EntityUid grid, Vector2 localPos, float range, out Vector2 target, out uint decalId)
    {
        target = default;
        decalId = 0;

        if (!TryComp<DecalGridComponent>(grid, out var decalGrid))
        {
            _tracked.Remove(grid);
            _seeded.Remove(grid);
            return false;
        }

        EnsureSeeded(grid, decalGrid);

        if (!_tracked.TryGetValue(grid, out var chunks))
            return false;

        var found = false;
        var bestDistSq = range * range;
        var originChunk = SharedDecalSystem.GetChunkIndices(localPos);

        for (var dx = -1; dx <= 1; dx++)
        {
            for (var dy = -1; dy <= 1; dy++)
            {
                var chunkIndex = originChunk + new Vector2i(dx, dy);
                if (!chunks.TryGetValue(chunkIndex, out var decals))
                    continue;

                SearchChunk(decalGrid, chunks, chunkIndex, decals, localPos,
                    ref bestDistSq, ref target, ref decalId, ref found);
            }
        }

        return found;
    }

    /// <summary>
    ///     Удаляет все очищаемые декали в радиусе <paramref name="radius"/> и чистит хэш
    /// </summary>
    public int CleanInRange(EntityUid grid, Vector2 localPos, float radius)
    {
        if (!TryComp<DecalGridComponent>(grid, out var decalGrid))
            return 0;

        EnsureSeeded(grid, decalGrid);

        if (!_tracked.TryGetValue(grid, out var chunks))
            return 0;

        _cleanBuffer.Clear();
        var radiusSq = radius * radius;
        var originChunk = SharedDecalSystem.GetChunkIndices(localPos);

        for (var dx = -1; dx <= 1; dx++)
        {
            for (var dy = -1; dy <= 1; dy++)
            {
                var chunkIndex = originChunk + new Vector2i(dx, dy);
                if (!chunks.TryGetValue(chunkIndex, out var decals))
                    continue;

                CollectInChunk(decalGrid, chunks, chunkIndex, decals, localPos, radiusSq, _cleanBuffer);
            }
        }

        // RemoveDecal шлёт DecalRemovedEvent ==> UnregisterByChunk (удаляет из хэша)
        foreach (var id in _cleanBuffer)
            _decals.RemoveDecal(grid, id);

        return _cleanBuffer.Count;
    }

    private void SearchChunk(
        DecalGridComponent decalGrid,
        Dictionary<Vector2i, Dictionary<uint, Vector2>> chunks,
        Vector2i chunkIndex,
        Dictionary<uint, Vector2> decals,
        Vector2 localPos,
        ref float bestDistSq,
        ref Vector2 target,
        ref uint bestId,
        ref bool found)
    {
        decalGrid.ChunkCollection.ChunkCollection.TryGetValue(chunkIndex, out var realChunk);

        _stale.Clear();

        foreach (var (id, coords) in decals)
        {
            if (IsStale(realChunk, id))
            {
                _stale.Add(id);
                continue;
            }

            var center = coords + new Vector2(0.5f, 0.5f);
            var distSq = (center - localPos).LengthSquared();
            if (distSq > bestDistSq)
                continue;

            bestDistSq = distSq;
            target = center;
            bestId = id;
            found = true;
        }

        PruneStale(chunks, chunkIndex, decals);
    }

    private void CollectInChunk(
        DecalGridComponent decalGrid,
        Dictionary<Vector2i, Dictionary<uint, Vector2>> chunks,
        Vector2i chunkIndex,
        Dictionary<uint, Vector2> decals,
        Vector2 localPos,
        float radiusSq,
        List<uint> output)
    {
        decalGrid.ChunkCollection.ChunkCollection.TryGetValue(chunkIndex, out var realChunk);

        _stale.Clear();

        foreach (var (id, coords) in decals)
        {
            if (IsStale(realChunk, id))
            {
                _stale.Add(id);
                continue;
            }

            var center = coords + new Vector2(0.5f, 0.5f);
            if ((center - localPos).LengthSquared() <= radiusSq)
                output.Add(id);
        }

        PruneStale(chunks, chunkIndex, decals);
    }

    // Декаль могли убрать в обход события.
    private static bool IsStale(DecalGridComponent.DecalChunk? realChunk, uint id)
        => realChunk == null || !realChunk.Decals.TryGetValue(id, out var decal) || !decal.Cleanable;

    private void PruneStale(
        Dictionary<Vector2i, Dictionary<uint, Vector2>> chunks,
        Vector2i chunkIndex,
        Dictionary<uint, Vector2> decals)
    {
        foreach (var id in _stale)
            decals.Remove(id);

        if (decals.Count == 0)
            chunks.Remove(chunkIndex);
    }
}
