// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Maths.FixedPoint;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Decals;
using Content.Shared.DoAfter;
using Content.Shared.Fluids;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Map.Components;

namespace Content.Goobstation.Shared.FloorCleaner;

// CorvaxGoob-Footprint-Refactoring 
// очистка сущностей удалена, т.к. следы переделаны в decals
// добавлена трата чистящего реагента для очистки декалей.
public sealed class FloorCleanerSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedDecalSystem _decal = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SharedPuddleSystem _puddle = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FloorCleanerComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<FloorCleanerComponent, FloorCleanerDoAfterEvent>(OnDoAfter);
    }

    private void OnAfterInteract(Entity<FloorCleanerComponent> floorCleaner, ref AfterInteractEvent args)
    {
        if (!args.CanReach
            || args.Handled
            || args.Target != null)
            return;

        // Уборщик чистит только декали (следы, грязь и т.п.). Следы больше не сущности.
        var gridUid = _transform.GetGrid(args.ClickLocation);

        if (!TryComp<MapGridComponent>(gridUid, out var mapgrid))
            return;

        var tileRef = _map.GetTileRef(gridUid.Value, mapgrid, args.ClickLocation);
        var foundDecals = _decal.GetDecalsInRange(tileRef.GridUid, tileRef.GridIndices, floorCleaner.Comp.Radius);

        foundDecals.RemoveWhere(decal => !decal.Decal.Cleanable);

        if (foundDecals.Count == 0)
            return;

        if (!HasEnoughCleaningReagent(floorCleaner, args.User))
        {
            args.Handled = true;
            return;
        }

        args.Handled = TryStartCleaning(floorCleaner, args.User, foundDecals);
    }

    private bool HasEnoughCleaningReagent(Entity<FloorCleanerComponent> floorCleaner, EntityUid user)
    {
        if (!TryComp<AbsorbentComponent>(floorCleaner, out var absorb))
            return true;

        if (!_solutionContainer.TryGetSolution(floorCleaner.Owner, absorb.SolutionName, out _, out var solution))
            return true;

        var reagents = _puddle.GetAbsorbentReagents(solution);
        var available = solution.GetTotalPrototypeQuantity(reagents);

        if (available >= FixedPoint2.New(floorCleaner.Comp.ReagentPerDecal))
            return true;

        _popup.PopupClient(Loc.GetString("cleaning-tool-no-reagent", ("used", floorCleaner.Owner)), user, user);
        return false;
    }

    private bool TryStartCleaning(
        Entity<FloorCleanerComponent> floorCleaner,
        EntityUid user,
        HashSet<(uint Index, Decal Decal)> decals)
    {
        var doAfterArgs = new DoAfterArgs(EntityManager,
            user,
            floorCleaner.Comp.CleanDelay,
            new FloorCleanerDoAfterEvent(decals),
            floorCleaner,
            used: floorCleaner)
        {
            NeedHand = true,
            BreakOnDamage = true,
            BreakOnMove = true,
            MovementThreshold = 0.01f,
        };

        _popup.PopupClient(Loc.GetString("cleaning-tool-scrubbing-start", ("user", user)), user);
        return _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnDoAfter(Entity<FloorCleanerComponent> floorCleaner, ref FloorCleanerDoAfterEvent args)
    {
        if (args.Handled
            || args.Cancelled)
            return;

        if (Transform(floorCleaner).GridUid is not { } grid)
            return;

        // Тратим чистящий раствор на каждую декаль.
        Entity<SolutionComponent>? cleanerSoln = null;
        if (TryComp<AbsorbentComponent>(floorCleaner, out var absorb))
            _solutionContainer.TryGetSolution(floorCleaner.Owner, absorb.SolutionName, out cleanerSoln, out _);

        var cost = FixedPoint2.New(floorCleaner.Comp.ReagentPerDecal);
        var consumed = false;

        foreach (var (index, _) in args.Decals)
        {
            if (cleanerSoln is { } soln)
            {
                var solution = soln.Comp.Solution;
                var reagents = _puddle.GetAbsorbentReagents(solution);

                // Раствор кончился - прекращаем чистку.
                if (solution.GetTotalPrototypeQuantity(reagents) < cost)
                    break;

                solution.SplitSolutionWithOnly(cost, reagents);
                consumed = true;
            }

            _decal.RemoveDecal(grid, index);
        }

        if (consumed && cleanerSoln is { } updatedSoln)
            _solutionContainer.UpdateChemicals(updatedSoln);
    }
}
