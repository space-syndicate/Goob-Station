using System.Numerics;
using Content.Goobstation.Maths.FixedPoint;
using Content.Goobstation.Shared.Vehicles;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Buckle.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Decals;
using Content.Shared.Fluids;
using Content.Shared.Movement.Components;
using Content.Shared.Popups;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared._CorvaxGoob.Janicart;

public sealed class JanicartCleanerSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedDecalSystem _decal = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedPuddleSystem _puddle = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private static readonly EntProtoId ToggleAction = "ActionToggleJanicartCleaning";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<JanicartCleanerComponent, StrappedEvent>(OnStrapped);
        SubscribeLocalEvent<JanicartCleanerComponent, UnstrappedEvent>(OnUnstrapped);
        SubscribeLocalEvent<JanicartCleanerComponent, ToggleJanicartCleaningActionEvent>(OnToggle);
        SubscribeLocalEvent<JanicartCleanerComponent, MoveEvent>(OnMove);
    }

    private void OnStrapped(Entity<JanicartCleanerComponent> ent, ref StrappedEvent args)
    {
        if (!HasComp<MobMoverComponent>(args.Buckle.Owner))
            return;

        _actions.AddAction(args.Buckle.Owner, ref ent.Comp.ToggleActionEntity, ToggleAction, ent);
        Dirty(ent);
    }

    private void OnUnstrapped(Entity<JanicartCleanerComponent> ent, ref UnstrappedEvent args)
    {
        if (!TryComp<ActionComponent>(ent.Comp.ToggleActionEntity, out var action)
            || action.AttachedEntity != args.Buckle.Owner)
            return;

        _actions.RemoveAction(args.Buckle.Owner, ent.Comp.ToggleActionEntity);
        SetEnabled(ent, false);
    }

    private void OnToggle(Entity<JanicartCleanerComponent> ent, ref ToggleJanicartCleaningActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        SetEnabled(ent, !ent.Comp.Enabled);

        var msg = ent.Comp.Enabled ? "janicart-cleaner-enabled" : "janicart-cleaner-disabled";
        _popup.PopupClient(Loc.GetString(msg, ("vehicle", ent.Owner)), ent, args.Performer);
    }

    private void OnMove(Entity<JanicartCleanerComponent> ent, ref MoveEvent args)
    {
        if (_net.IsClient || !ent.Comp.Enabled)
            return;

        if (!TryComp<VehicleComponent>(ent, out var vehicle) || vehicle.Driver == null)
            return;

        var gridUid = _transform.GetGrid(args.NewPosition);
        if (gridUid == null || !TryComp<MapGridComponent>(gridUid, out var grid))
            return;

        var tile = _map.TileIndicesFor(gridUid.Value, grid, args.NewPosition);
        if (args.OldPosition.EntityId == args.NewPosition.EntityId
            && tile == _map.TileIndicesFor(gridUid.Value, grid, args.OldPosition))
            return;

        if (!_solution.TryGetRefillableSolution(ent.Owner, out var tank, out var solution))
            return;

        var dose = solution.SplitSolutionWithOnly(ent.Comp.DosePerTile, ent.Comp.Reagents);
        if (dose.Volume == FixedPoint2.Zero)
            return;

        CleanDecals(ent, gridUid.Value, tile, dose);
        _puddle.DoTileReactions(_map.GetTileRef(gridUid.Value, grid, tile), dose);
        solution.AddSolution(dose, _proto);
        _solution.UpdateChemicals(tank.Value);
    }

    private void CleanDecals(Entity<JanicartCleanerComponent> ent, EntityUid grid, Vector2i tile, Solution dose)
    {
        var water = _puddle.GetAbsorbentReagents(dose);
        if (water.Length == 0)
            return;

        var decals = _decal.GetDecalsInRange(grid, tile + new Vector2(0.5f, 0.5f), validDelegate: d => d.Cleanable);
        foreach (var (index, _) in decals)
        {
            if (dose.GetTotalPrototypeQuantity(water) < ent.Comp.WaterPerDecal)
                break;

            dose.SplitSolutionWithOnly(ent.Comp.WaterPerDecal, water);
            _decal.RemoveDecal(grid, index);
        }
    }

    private void SetEnabled(Entity<JanicartCleanerComponent> ent, bool enabled)
    {
        if (ent.Comp.Enabled == enabled)
            return;

        ent.Comp.Enabled = enabled;
        Dirty(ent);
        _actions.SetToggled(ent.Comp.ToggleActionEntity, enabled);
    }
}
