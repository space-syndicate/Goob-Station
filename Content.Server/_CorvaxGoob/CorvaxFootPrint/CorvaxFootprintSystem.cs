using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Goobstation.Common.Footprints;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Goobstation.Maths.FixedPoint;
using Content.Shared.Fluids.Components;
using Content.Shared.Standing;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Content.Server.Gravity;
using Content.Server.Decals;
using Content.Goobstation.Common.CCVar;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Configuration;
using Content.Server._CorvaxGoob.Footprints.Components;

namespace Content.Server._CorvaxGoob.Footprints;

public sealed class CorvaxFootprintSystem : EntitySystem
{
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly GravitySystem _gravity = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] private readonly DecalSystem _decals = default!;

    private EntityQuery<CannotLeaveTracesComponent> _noFootprintsQuery = default!;

    public const string FootPrintSolution = "print";

    public const string PuddleSolution = "puddle";

    private float _minimumPuddleSize;

    // Кэшированный набор реагентов, прилипающих к коже, восстанавливается при перезагрузке прототипа.
    // Позволяет избежать классификации каждого реагента в каждом следе.
    private readonly HashSet<ProtoId<ReagentPrototype>> _stickyReagents = new();

    // Буфер для липких реагентов, присутствующих в луже.
    private readonly List<ProtoId<ReagentPrototype>> _stickyBuffer = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<CorvaxFootprintOwnerComponent, MoveEvent>(OnMove);
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);

        _noFootprintsQuery = GetEntityQuery<CannotLeaveTracesComponent>();

        Subs.CVar(_configuration, GoobCVars.MinimumPuddleSizeForFootprints, value => _minimumPuddleSize = value, true);

        CacheSticky();
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs ev)
    {
        if (ev.WasModified<ReagentPrototype>())
            CacheSticky();
    }

    private void CacheSticky()
    {
        _stickyReagents.Clear();
        foreach (var proto in _prototype.EnumeratePrototypes<ReagentPrototype>())
            if (proto.SticksToSkin)
                _stickyReagents.Add(proto.ID);
    }

    private void OnMove(Entity<CorvaxFootprintOwnerComponent> entity, ref MoveEvent e)
    {
        if (_noFootprintsQuery.HasComp(entity))
            return;

        if (!e.OldPosition.IsValid(EntityManager) || !e.NewPosition.IsValid(EntityManager))
            return;

        entity.Comp.Distance += GetDelta(e).Length();

        var standing = TryComp<StandingStateComponent>(entity, out var standingState) && standingState.Standing;
        var requiredDistance = standing ? entity.Comp.FootDistance : entity.Comp.BodyDistance;

        if (entity.Comp.Distance < requiredDistance)
            return;

        if (_gravity.IsWeightless(entity.Owner))
            return;

        entity.Comp.Distance -= requiredDistance;

        var attemptEv = new FootprintLeaveAttemptEvent(entity.Owner);
        RaiseLocalEvent(ref attemptEv);
        if (attemptEv.Cancelled)
            return;

        var transform = Transform(entity);

        if (transform.GridUid is null)
            return;

        if (!TryComp<MapGridComponent>(transform.GridUid.Value, out var gridComponent))
            return;

        EntityCoordinates coordinates = new(entity, standing ? entity.Comp.NextFootOffset : 0, 0);

        entity.Comp.NextFootOffset = -entity.Comp.NextFootOffset;

        var tile = _map.CoordinatesToTile(transform.GridUid.Value, gridComponent, coordinates);

        if (TryPuddleInteraction(entity, (transform.GridUid.Value, gridComponent), tile, standing))
            return;

        Angle rotation;

        if (!standing)
        {
            Vector2 gridLocalDelta;

            if (e.OldPosition.EntityId == transform.GridUid && e.NewPosition.EntityId == transform.GridUid)
            {
                // родитель - сам грид, координаты уже локальны к нему
                gridLocalDelta = e.NewPosition.Position - e.OldPosition.Position;
            }
            else
            {
                var oldLocal = _map.WorldToLocal(transform.GridUid.Value, gridComponent, _transform.ToMapCoordinates(e.OldPosition).Position);
                var newLocal = _map.WorldToLocal(transform.GridUid.Value, gridComponent, _transform.ToMapCoordinates(e.NewPosition).Position);
                gridLocalDelta = newLocal - oldLocal;
            }

            rotation = gridLocalDelta.ToAngle();
        }
        else
            rotation = transform.LocalRotation;

        FootprintInteraction(entity, transform.GridUid.Value, coordinates, rotation, standing);
    }

    private Vector2 GetDelta(in MoveEvent e)
    {
        if (e.OldPosition.EntityId == e.NewPosition.EntityId)
            return e.NewPosition.Position - e.OldPosition.Position;

        return
        _transform.ToMapCoordinates(e.NewPosition).Position
            -
        _transform.ToMapCoordinates(e.OldPosition).Position;
    }

    private bool TryPuddleInteraction(Entity<CorvaxFootprintOwnerComponent> entity, Entity<MapGridComponent> grid, Vector2i tile, bool standing)
    {
        if (!TryGetAnchoredEntity<PuddleComponent>(grid, tile, out var puddle))
            return false;

        if (!_solution.TryGetSolution(puddle.Value.Owner, PuddleSolution, out var puddleSolution, out var puddleSol))
            return false;

        // Собираем реагенты в луже, которые прилипают к ногам, используя кэшированную
        // классификацию, чтобы не выделять словарь/список для каждого следа.
        _stickyBuffer.Clear();
        var stickyVolume = FixedPoint2.Zero;
        foreach (var (reagent, quantity) in puddleSol.Contents)
        {
            if (!_stickyReagents.Contains(reagent.Prototype))
                continue;

            _stickyBuffer.Add(reagent.Prototype);
            stickyVolume += quantity;
        }

        // В луже ничто не может прилипнуть к ногам, поэтому ничего не следует подбирать
        if (_stickyBuffer.Count == 0)
            return false;

        if (!_solution.EnsureSolutionEntity(entity.Owner, FootPrintSolution,
                out _,
                out var solution,
                FixedPoint2.Max(entity.Comp.MaxFootVolume, entity.Comp.MaxBodyVolume)))
            return false;

        var feetSol = solution.Value.Comp.Solution;

        // Добавление в лужу количество того, что уже находится на ногах.
        var deposit = feetSol.SplitSolution(GetFootprintVolume(entity, solution.Value));
        puddleSol.AddSolution(deposit, _prototype);

        // Следы оставляют только в том случае, если в луже содержится 
        // достаточное количество вещества, прилипающего к ногам, чтобы их образовать.
        if (stickyVolume < _minimumPuddleSize)
        {
            _solution.UpdateChemicals(puddleSolution.Value, false);
            return false;
        }

        var topUp = FixedPoint2.Max(0, (standing ? entity.Comp.MaxFootVolume : entity.Comp.MaxBodyVolume) - feetSol.Volume);
        if (topUp > 0)
            feetSol.AddSolution(puddleSol.SplitSolutionWithOnly(topUp, _stickyBuffer.ToArray()), _prototype);

        _solution.UpdateChemicals(solution.Value, false);
        _solution.UpdateChemicals(puddleSolution.Value, false);

        return true;
    }

    private void FootprintInteraction(Entity<CorvaxFootprintOwnerComponent> entity, EntityUid gridUid, EntityCoordinates footCoordinates, Angle rotation, bool standing)
    {
        if (!_solution.TryGetSolution(entity.Owner, FootPrintSolution, out var solution, out _))
            return;

        var volume = standing ? GetFootprintVolume(entity, solution.Value) : GetBodyprintVolume(entity, solution.Value);

        if (volume < entity.Comp.MinFootprintVolume)
        {
            // часть раствора остаётся навсегда, что вызывает неожиданное поведение;
            // удаление всего раствора после остановки следов решает проблему, потеря реагента незначительна
            _solution.RemoveAllSolution(solution.Value);
            return;
        }

        // Следы - косметические декали, поэтому реагент тратится с ног здесь, а не хранится на следе.
        // Т.е. реагент просто исчезает, это жертва нужна для производительности системы.
        // Мытьё следа больше не возвращает реагент в лужу (этим занимается CleanDecalsReaction).
        var spent = _solution.SplitSolution(solution.Value, volume);

        var maxPrint = standing ? entity.Comp.MaxFootprintVolume : entity.Comp.MaxBodyprintVolume;
        var color = spent.GetColor(_prototype).WithAlpha((float)volume / (float)maxPrint / 2f);

        var decalId = standing ? entity.Comp.FootDecalId : entity.Comp.BodyDecalId;
        var angle = Quantize(rotation + Angle.FromDegrees(entity.Comp.SpriteAngleOffset), entity.Comp);

        // Позиция ноги в локальных координатах грида (декали хранятся относительно грида).
        var center = _transform.ToCoordinates(gridUid, _transform.ToMapCoordinates(footCoordinates)).Position;

        PlaceOrMergeDecal(gridUid, center, angle, color, decalId, entity.Comp);
    }

    /// <summary>
    /// Ставит декаль следа или сливает её с ближайшей, которая уже смотрит в то же квантованное
    /// направление, чтобы перекрывающиеся следы уплотняли одну декаль, а не плодили множество сущностей.
    /// </summary>
    private void PlaceOrMergeDecal(EntityUid gridUid, Vector2 center, Angle angle, Color color, string decalId, CorvaxFootprintOwnerComponent comp)
    {
        foreach (var (index, decal) in _decals.GetDecalsInRange(gridUid, center, comp.GroupRadius,
                     d => d.Id == decalId && AnglesEqual(d.Angle, angle)))
        {
            var old = decal.Color ?? color;
            var mixed = Color.InterpolateBetween(old, color, comp.DecalBlendFactor);
            var alpha = MathF.Min(1f, old.A + color.A * comp.DecalAlphaAccumulation);
            _decals.SetDecalColor(gridUid, index, mixed.WithAlpha(alpha));
            return;
        }

        // Координата декали - нижний левый угол квада 1x1, поэтому смещаем на половину тайла для центровки на ноге.
        var coords = new EntityCoordinates(gridUid, center - new Vector2(0.5f, 0.5f));
        _decals.TryAddDecal(decalId, coords, out _, color, angle, cleanable: true);
    }

    /// <summary>
    /// Прижимает угол к ближайшему из <see cref="CorvaxFootprintOwnerComponent.DirectionCount"/> равномерно
    /// распределённых направлений. Офсет по умолчанию - половина шага, например 45/135/225/315 для четырёх направлений.
    /// </summary>
    private static Angle Quantize(Angle angle, CorvaxFootprintOwnerComponent comp)
    {
        var count = Math.Max(1, comp.DirectionCount);
        var step = Math.Tau / count;
        var offset = comp.DirectionOffsetDegrees is { } deg ? Angle.FromDegrees(deg).Theta : step / 2.0;

        var theta = (angle.Theta - offset) % Math.Tau;
        if (theta < 0)
            theta += Math.Tau;

        var snapped = Math.Round(theta / step) * step;
        return new Angle((snapped + offset) % Math.Tau);
    }

    private static bool AnglesEqual(Angle a, Angle b)
        => Math.Abs(Angle.ShortestDistance(a, b).Theta) < 0.01;

    private static FixedPoint2 GetFootprintVolume(Entity<CorvaxFootprintOwnerComponent> entity, Entity<SolutionComponent> solution)
    {
        return FixedPoint2.Min(solution.Comp.Solution.Volume, (entity.Comp.MaxFootprintVolume - entity.Comp.MinFootprintVolume) * (solution.Comp.Solution.Volume / entity.Comp.MaxFootVolume) + entity.Comp.MinFootprintVolume);
    }

    private static FixedPoint2 GetBodyprintVolume(Entity<CorvaxFootprintOwnerComponent> entity, Entity<SolutionComponent> solution)
    {
        return FixedPoint2.Min(solution.Comp.Solution.Volume, (entity.Comp.MaxBodyprintVolume - entity.Comp.MinBodyprintVolume) * (solution.Comp.Solution.Volume / entity.Comp.MaxBodyVolume) + entity.Comp.MinBodyprintVolume);
    }

    private bool TryGetAnchoredEntity<T>(Entity<MapGridComponent> grid, Vector2i pos, [NotNullWhen(true)] out Entity<T>? entity) where T : IComponent
    {
        var anchoredEnumerator = _map.GetAnchoredEntitiesEnumerator(grid, grid, pos);
        var entityQuery = GetEntityQuery<T>();

        while (anchoredEnumerator.MoveNext(out var ent))
            if (entityQuery.TryComp(ent, out var comp))
            {
                entity = (ent.Value, comp);
                return true;
            }

        entity = null;
        return false;
    }
}
