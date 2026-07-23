using System.Numerics;
using Content.Shared.Imperial.XxRaay.Components;
using Content.Shared.Imperial.XxRaay.DataDefinitions;
using Content.Server.Body.Systems;
using Content.Server.Explosion.EntitySystems;
using Content.Shared.Polymorph;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Stealth;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.CombatMode;
using Content.Shared.Actions.Components;
using Content.Shared.DoAfter;
using Content.Shared.Eye;
using Content.Shared.Imperial.XxRaay.Events;
using Content.Shared.Imperial.XxRaay.Systems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.NodeContainer;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Stealth;
using Content.Shared.Stealth.Components;
using Content.Shared.Tools.Systems;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Content.Shared.Atmos;
namespace Content.Server.Imperial.XxRaay.Systems;

public sealed class ImperialVentCrawlerSystem : SharedImperialVentCrawlerSystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedCombatModeSystem _combatMode = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly StealthSystem _stealth = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly VisibilitySystem _visibility = default!;
    [Dependency] private readonly WeldableSystem _weldable = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ExplosionSystem _explosion = default!;

    private EntityQuery<ActiveWormCorpsePossessionComponent> _corpsePossessionQuery;
    private EntityQuery<ActiveWormDoorHidingComponent> _doorHidingQuery;
    private EntityQuery<WormBloodDrinkingComponent> _drinkingQuery;
    private EntityQuery<ActiveWormEvolutionComponent> _evolutionQuery;
    private EntityQuery<ActiveWormReproductionComponent> _reproductionQuery;

    public override void Initialize()
    {
        base.Initialize();

        _corpsePossessionQuery = GetEntityQuery<ActiveWormCorpsePossessionComponent>();
        _doorHidingQuery = GetEntityQuery<ActiveWormDoorHidingComponent>();
        _drinkingQuery = GetEntityQuery<WormBloodDrinkingComponent>();
        _evolutionQuery = GetEntityQuery<ActiveWormEvolutionComponent>();
        _reproductionQuery = GetEntityQuery<ActiveWormReproductionComponent>();

        SubscribeLocalEvent<VentCrawlPointComponent, GetVerbsEvent<InteractionVerb>>(OnVentGetVerbs);
        SubscribeLocalEvent<VentCrawlPointComponent, EnterImperialVentCrawlerDoAfterEvent>(OnEnterDoAfter);
        SubscribeLocalEvent<VentCrawlPointComponent, ExitImperialVentCrawlerDoAfterEvent>(OnExitDoAfter);

        SubscribeLocalEvent<VentCrawlingComponent, GetVisMaskEvent>(OnGetVisMask);
        SubscribeLocalEvent<VentCrawlingComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMoveSpeed);
        SubscribeLocalEvent<VentCrawlingComponent, MoveEvent>(OnMoved);
        SubscribeLocalEvent<VentCrawlingComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<VentCrawlingComponent, VentCrawlTileMoveAttemptEvent>(OnVentCrawlTileMoveAttempt);

        SubscribeLocalEvent<VentCrawlingComponent, InhaleLocationEvent>(OnInhale);
        SubscribeLocalEvent<VentCrawlingComponent, ExhaleLocationEvent>(OnExhale);
        SubscribeLocalEvent<VentCrawlingComponent, AtmosExposedGetAirEvent>(OnExposed);
        SubscribeLocalEvent<VentCrawlingComponent, PolymorphedEvent>(OnPolymorphed);
    }

    private void OnInhale(Entity<VentCrawlingComponent> ent, ref InhaleLocationEvent args)
    {
        if (TryGetPipeNode(ent.Comp.SourceVent, out var pipeNode))
        {
            args.Gas = pipeNode.Air;
        }
    }

    private void OnExhale(Entity<VentCrawlingComponent> ent, ref ExhaleLocationEvent args)
    {
        if (TryGetPipeNode(ent.Comp.SourceVent, out var pipeNode))
        {
            args.Gas = pipeNode.Air;
        }
    }

    private void OnExposed(Entity<VentCrawlingComponent> ent, ref AtmosExposedGetAirEvent args)
    {
        if (args.Handled)
            return;

        if (TryGetPipeNode(ent.Comp.SourceVent, out var pipeNode))
        {
            args.Gas = pipeNode.Air;
            args.Handled = true;
        }
    }

    private void OnPolymorphed(Entity<VentCrawlingComponent> ent, ref PolymorphedEvent args)
    {
        if (!args.IsRevert) return;

        var vent = ent.Comp.SourceVent;

        if (TryGetGridTile(Transform(ent).Coordinates, out var gridUid, out var grid, out var tile))
        {
            foreach (var anchored in _map.GetAnchoredEntities((gridUid, grid), tile))
            {
                if (SharesPipeNetwork(anchored, vent))
                {
                    _explosion.QueueExplosion(anchored, ExplosionSystem.DefaultExplosionPrototypeId, 10, 2, 2);
                    QueueDel(anchored);
                    break;
                }
            }
        }
    }

    private void OnVentCrawlTileMoveAttempt(Entity<VentCrawlingComponent> ent, ref VentCrawlTileMoveAttemptEvent args)
    {
        if (!TryComp(args.GridUid, out MapGridComponent? grid))
            return;

        var targetCoords = _map.ToCenterCoordinates(args.GridUid, args.TargetTile, grid);
        args.CanMove = IsOnPipeNetwork(ent.Comp, targetCoords);
    }

    private void OnVentGetVerbs(Entity<VentCrawlPointComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        AddVentVerbs(ent, ref args);
    }

    private void OnEnterDoAfter(Entity<VentCrawlPointComponent> ent, ref EnterImperialVentCrawlerDoAfterEvent args)
    {
        HandleEnterDoAfter(ent, ref args);
    }

    private void OnExitDoAfter(Entity<VentCrawlPointComponent> ent, ref ExitImperialVentCrawlerDoAfterEvent args)
    {
        HandleExitDoAfter(ent, ref args);
    }

    private void AddVentVerbs(EntityUid vent, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!_actionBlocker.CanConsciouslyPerformAction(args.User) || !_interaction.InRangeUnobstructed(args.User, vent))
            return;

        if (!TryComp(args.User, out ImperialVentCrawlerComponent? crawler))
            return;

        var user = args.User;

        if (CanEnterVent(user, vent))
        {
            args.Verbs.Add(new InteractionVerb
            {
                Priority = crawler.EnterVerbPriority,
                Text = Loc.GetString(crawler.EnterVerb),
                Icon = crawler.EnterVerbIcon,
                DoContactInteraction = false,
                Act = () => StartEnterVent(user, vent, crawler),
            });
        }

        if (TryComp(user, out VentCrawlingComponent? active) && CanExitVent(user, vent, active))
        {
            args.Verbs.Add(new InteractionVerb
            {
                Priority = crawler.ExitVerbPriority,
                Text = Loc.GetString(crawler.ExitVerb),
                Icon = crawler.ExitVerbIcon,
                DoContactInteraction = false,
                Act = () => StartExitVent(user, vent, crawler),
            });
        }
    }

    private void StartEnterVent(EntityUid user, EntityUid vent, ImperialVentCrawlerComponent crawler)
    {
        if (!CanEnterVent(user, vent, showPopup: true) || !_actionBlocker.CanConsciouslyPerformAction(user))
            return;

        if (!_interaction.InRangeUnobstructed(user, vent))
            return;

        var args = new DoAfterArgs(EntityManager, user, crawler.EnterDelay, new EnterImperialVentCrawlerDoAfterEvent(), vent, target: vent)
        {
            BreakOnMove = true,
            BreakOnWeightlessMove = true,
            BreakOnDamage = true,
            NeedHand = false,
        };

        _doAfter.TryStartDoAfter(args);
    }

    private void HandleEnterDoAfter(EntityUid vent, ref EnterImperialVentCrawlerDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (!TryComp(args.User, out ImperialVentCrawlerComponent? _) || !CanEnterVent(args.User, vent))
            return;

        if (!_actionBlocker.CanConsciouslyPerformAction(args.User) || !_interaction.InRangeUnobstructed(args.User, vent))
            return;

        EnterVent(args.User, vent);
        _combatMode.SetInCombatMode(args.User, false);
        args.Handled = true;
    }

    private void StartExitVent(EntityUid user, EntityUid vent, ImperialVentCrawlerComponent crawler)
    {
        if (!TryComp(user, out VentCrawlingComponent? active) || !CanExitVent(user, vent, active, showPopup: true))
            return;

        var args = new DoAfterArgs(EntityManager, user, crawler.ExitDelay, new ExitImperialVentCrawlerDoAfterEvent(), vent, target: vent)
        {
            BreakOnMove = true,
            BreakOnWeightlessMove = true,
            BreakOnDamage = true,
            NeedHand = false,
            RequireCanInteract = false,
        };

        _doAfter.TryStartDoAfter(args);
    }

    private void HandleExitDoAfter(EntityUid vent, ref ExitImperialVentCrawlerDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (!TryComp(args.User, out VentCrawlingComponent? active) || !CanExitVent(args.User, vent, active))
            return;

        ExitVent(args.User, vent);
        args.Handled = true;
    }

    private bool CanEnterVent(EntityUid user, EntityUid vent, bool showPopup = false)
    {
        if (HasComp<VentCrawlingComponent>(user))
        {
            if (showPopup)
                ShowFailPopup(user, Loc.GetString("vent-crawler-fail-in-vent"));
            return false;
        }

        if (_corpsePossessionQuery.HasComp(user))
        {
            if (showPopup)
                ShowFailPopup(user, Loc.GetString("vent-crawler-fail-corpse"));
            return false;
        }

        if (_doorHidingQuery.HasComp(user))
        {
            if (showPopup)
                ShowFailPopup(user, Loc.GetString("vent-crawler-fail-door"));
            return false;
        }

        if (_drinkingQuery.HasComp(user))
        {
            if (showPopup)
                ShowFailPopup(user, Loc.GetString("vent-crawler-fail-drinking"));
            return false;
        }

        if (_evolutionQuery.HasComp(user))
        {
            if (showPopup)
                ShowFailPopup(user, Loc.GetString("vent-crawler-fail-evolving"));
            return false;
        }

        if (_reproductionQuery.HasComp(user))
        {
            if (showPopup)
                ShowFailPopup(user, Loc.GetString("vent-crawler-fail-reproducing"));
            return false;
        }

        if (!Transform(vent).Anchored)
        {
            if (showPopup)
                ShowFailPopup(user, Loc.GetString("vent-crawler-fail-invalid"));
            return false;
        }

        if (_weldable.IsWelded(vent))
        {
            if (showPopup)
                ShowFailPopup(user, Loc.GetString("vent-crawler-fail-welded"));
            return false;
        }

        if (!TryGetPipeNode(vent, out _))
        {
            if (showPopup)
                ShowFailPopup(user, Loc.GetString("vent-crawler-fail-invalid"));
            return false;
        }

        return true;
    }

    private void ShowFailPopup(EntityUid user, string message)
    {
        _popup.PopupEntity(message, user, user, PopupType.Small);
    }

    private bool CanExitVent(EntityUid user, EntityUid vent, VentCrawlingComponent active, bool showPopup = false)
    {
        if (_weldable.IsWelded(vent))
        {
            if (showPopup)
                ShowFailPopup(user, Loc.GetString("vent-crawler-fail-welded"));
            return false;
        }

        return IsOnSameTile(user, vent) && SharesPipeNetwork(vent, active.SourceVent);
    }

    public void EnterVent(EntityUid user, EntityUid vent)
    {
        if (!TryComp(user, out ImperialVentCrawlerComponent? crawler))
            return;

        var active = AddComp<VentCrawlingComponent>(user);
        EnsureComp<ActiveVentCrawlingComponent>(user);

        active.SourceVent = vent;
        active.RemovedComplexInteraction = false;
        active.WasCollidable = true;
        active.FixtureStates.Clear();
        active.AddedStealth = false;
        active.AddedVisibility = false;
        active.PreviousStealthEnabled = true;
        active.PreviousStealthVisibility = 1f;
        active.PreviousVisibilityLayer = crawler.HiddenVisibilityLayer;
        active.SoundDistance = 0f;
        active.DisabledActions.Clear();

        if (TryComp(user, out PhysicsComponent? physics))
        {
            active.WasCollidable = physics.CanCollide;
            SaveAndClearFixtures(user, active, physics);

            if (physics.CanCollide)
                _physics.SetCanCollide(user, false, body: physics);

            _physics.ResetDynamics(user, physics);
        }

        if (crawler.UseStealth)
            ApplyStealth(user, active, crawler);

        ApplyVentVisibility(user, active, crawler);

        if (crawler.RemoveComplexInteraction)
            RemoveComplexInteraction(user, active);

        if (crawler.DisableActions)
            SetActionsEnabled(user, false, active);

        _transform.SetCoordinates(user, Transform(user), Transform(vent).Coordinates);
        _movement.RefreshMovementSpeedModifiers(user);

        EnsureComp<EntityTileMovementComponent>(user);

        if (TryComp(user, out EyeComponent? eye))
            _eye.RefreshVisibilityMask((user, eye));
    }

    private void ExitVent(EntityUid user, EntityUid vent)
    {
        if (!TryComp(user, out VentCrawlingComponent? active) || !CanExitVent(user, vent, active))
            return;

        _transform.SetCoordinates(user, Transform(user), Transform(vent).Coordinates);
        RemComp<VentCrawlingComponent>(user);
    }

    private void OnMoved(Entity<VentCrawlingComponent> ent, ref MoveEvent args)
    {
        TryPlayVentCrawlSound(ent, ref args);
    }

    private void OnGetVisMask(Entity<VentCrawlingComponent> ent, ref GetVisMaskEvent args)
    {
        if (!TryComp(ent, out ImperialVentCrawlerComponent? crawler))
            return;

        if (crawler.EyeSeeSubfloor)
            args.VisibilityMask |= (int) VisibilityFlags.Subfloor;

        if (crawler.EyeSeeCrawlVisibilityLayer)
            args.VisibilityMask |= (int) crawler.CrawlVisibilityLayer;
    }

    private void OnShutdown(Entity<VentCrawlingComponent> ent, ref ComponentShutdown args)
    {
        DisableTileMovement(ent);

        if (TryComp(ent, out PhysicsComponent? physics))
        {
            RestoreFixtures(ent, ent.Comp, physics);
            _physics.SetCanCollide(ent, ent.Comp.WasCollidable, body: physics);
            _physics.ResetDynamics(ent, physics);
        }

        if (TryComp(ent, out ImperialVentCrawlerComponent? crawler))
        {
            if (crawler.RemoveComplexInteraction)
                RestoreComplexInteraction(ent, ent.Comp);

            RestoreVisibility(ent, ent.Comp);

            if (crawler.UseStealth)
                RestoreStealth(ent, ent.Comp);

            if (crawler.DisableActions)
                SetActionsEnabled(ent, true, ent.Comp);
        }

        RemComp<ActiveVentCrawlingComponent>(ent);
        _movement.RefreshMovementSpeedModifiers(ent);

        if (TryComp(ent, out EyeComponent? eye))
            _eye.RefreshVisibilityMask((ent, eye));
    }

    private void OnRefreshMoveSpeed(Entity<VentCrawlingComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (!TryComp(ent, out ImperialVentCrawlerComponent? crawler))
            return;

        args.ModifySpeed(crawler.VentSpeedMultiplier, crawler.VentSpeedMultiplier);
    }

    private bool IsOnPipeNetwork(VentCrawlingComponent active, EntityCoordinates coordinates)
    {
        if (!TryGetPipeNode(active.SourceVent, out var sourcePipe))
            return false;

        if (!TryGetGridTile(coordinates, out var gridUid, out var grid, out var tile))
            return false;

        foreach (var anchored in _map.GetAnchoredEntities((gridUid, grid), tile))
        {
            if (SharesPipeNetwork(anchored, active.SourceVent, sourcePipe))
                return true;
        }

        return false;
    }

    private bool SharesPipeNetwork(EntityUid entity, EntityUid sourceVent)
    {
        if (!TryGetPipeNode(sourceVent, out var sourcePipe))
            return false;

        return SharesPipeNetwork(entity, sourceVent, sourcePipe);
    }

    private bool SharesPipeNetwork(EntityUid entity, EntityUid sourceVent, PipeNode sourcePipe)
    {
        if (!TryComp(entity, out NodeContainerComponent? nodeContainer))
            return false;

        foreach (var node in nodeContainer.Nodes.Values)
        {
            if (node is not PipeNode pipe)
                continue;

            if (sourcePipe.NodeGroup == null)
                return entity == sourceVent;

            if (ReferenceEquals(pipe.NodeGroup, sourcePipe.NodeGroup))
                return true;
        }

        return false;
    }

    private bool TryGetPipeNode(EntityUid uid, out PipeNode pipeNode)
    {
        pipeNode = default!;

        if (!TryComp(uid, out NodeContainerComponent? nodeContainer))
            return false;

        foreach (var node in nodeContainer.Nodes.Values)
        {
            if (node is not PipeNode pipe)
                continue;

            pipeNode = pipe;
            return true;
        }

        return false;
    }

    private bool TryGetGridTile(EntityCoordinates coordinates, out EntityUid gridUid, out MapGridComponent grid, out Vector2i tile)
    {
        gridUid = default;
        grid = default!;
        tile = default;

        if (!TryComp(coordinates.EntityId, out MapGridComponent? gridComp))
            return false;

        grid = gridComp;
        gridUid = coordinates.EntityId;
        tile = _map.TileIndicesFor(gridUid, grid, coordinates);
        return true;
    }

    private bool IsOnSameTile(EntityUid first, EntityUid second)
    {
        if (!TryGetGridTile(Transform(first).Coordinates, out var firstGrid, out _, out var firstTile))
            return false;

        if (!TryGetGridTile(Transform(second).Coordinates, out var secondGrid, out _, out var secondTile))
            return false;

        return firstGrid == secondGrid && firstTile == secondTile;
    }

    private void ApplyStealth(EntityUid user, VentCrawlingComponent active, ImperialVentCrawlerComponent crawler)
    {
        var hadStealth = TryComp(user, out StealthComponent? stealth);
        if (!hadStealth)
        {
            stealth = EnsureComp<StealthComponent>(user);
            active.AddedStealth = true;
        }

        active.PreviousStealthEnabled = stealth!.Enabled;
        active.PreviousStealthVisibility = _stealth.GetVisibility(user, stealth);

        _stealth.SetEnabled(user, true, stealth);
        _stealth.SetVisibility(user, crawler.StealthVisibility, stealth);
    }

    private void RestoreStealth(EntityUid user, VentCrawlingComponent active)
    {
        if (!TryComp(user, out StealthComponent? stealth))
            return;

        if (active.AddedStealth)
        {
            RemComp<StealthComponent>(user);
            return;
        }

        _stealth.SetEnabled(user, active.PreviousStealthEnabled, stealth);
        _stealth.SetVisibility(user, active.PreviousStealthVisibility, stealth);
    }

    private void ApplyVentVisibility(EntityUid user, VentCrawlingComponent active, ImperialVentCrawlerComponent crawler)
    {
        var hadVisibility = TryComp(user, out VisibilityComponent? visibility);
        if (!hadVisibility)
        {
            visibility = EnsureComp<VisibilityComponent>(user);
            active.AddedVisibility = true;
        }

        active.PreviousVisibilityLayer = visibility!.Layer;
        _visibility.AddLayer((user, visibility), crawler.CrawlVisibilityLayer, false);
        _visibility.RemoveLayer((user, visibility), crawler.HiddenVisibilityLayer, false);
        _visibility.RefreshVisibility(user, visibilityComponent: visibility);
    }

    private void RestoreVisibility(EntityUid user, VentCrawlingComponent active)
    {
        if (!TryComp(user, out VisibilityComponent? visibility))
            return;

        if (active.AddedVisibility)
        {
            RemComp<VisibilityComponent>(user);
            return;
        }

        _visibility.SetLayer((user, visibility), active.PreviousVisibilityLayer, false);
        _visibility.RefreshVisibility(user, visibilityComponent: visibility);
    }

    private void RemoveComplexInteraction(EntityUid user, VentCrawlingComponent active)
    {
        if (!HasComp<ComplexInteractionComponent>(user))
            return;

        active.RemovedComplexInteraction = true;
        RemComp<ComplexInteractionComponent>(user);
    }

    private void RestoreComplexInteraction(EntityUid user, VentCrawlingComponent active)
    {
        if (!active.RemovedComplexInteraction)
            return;

        EnsureComp<ComplexInteractionComponent>(user);
        active.RemovedComplexInteraction = false;
    }

    private void SaveAndClearFixtures(EntityUid user, VentCrawlingComponent active, PhysicsComponent physics)
    {
        if (!TryComp(user, out FixturesComponent? fixtures))
            return;

        active.FixtureStates.Clear();

        foreach (var (id, fixture) in fixtures.Fixtures)
        {
            active.FixtureStates.Add(new ImperialVentCrawlerFixtureState
            {
                Id = id,
                Hard = fixture.Hard,
                CollisionLayer = (CollisionGroup) fixture.CollisionLayer,
                CollisionMask = (CollisionGroup) fixture.CollisionMask,
            });

            _physics.SetHard(user, fixture, false, fixtures);
            _physics.SetCollisionLayer(user, id, fixture, (int) CollisionGroup.None, fixtures, physics);
            _physics.SetCollisionMask(user, id, fixture, (int) CollisionGroup.None, fixtures, physics);
        }
    }

    private void RestoreFixtures(EntityUid user, VentCrawlingComponent active, PhysicsComponent physics)
    {
        if (!TryComp(user, out FixturesComponent? fixtures))
            return;

        foreach (var state in active.FixtureStates)
        {
            if (!fixtures.Fixtures.TryGetValue(state.Id, out var fixture))
                continue;

            _physics.SetHard(user, fixture, state.Hard, fixtures);
            _physics.SetCollisionLayer(user, state.Id, fixture, (int) state.CollisionLayer, fixtures, physics);
            _physics.SetCollisionMask(user, state.Id, fixture, (int) state.CollisionMask, fixtures, physics);
        }

        active.FixtureStates.Clear();
    }

    private void SetActionsEnabled(EntityUid user, bool enabled, VentCrawlingComponent active)
    {
        if (!enabled)
        {
            foreach (var action in _actions.GetActions(user))
            {
                if (!action.Comp.Enabled)
                    continue;

                active.DisabledActions.Add(action.Owner);
                _actions.SetEnabled(new Entity<ActionComponent?>(action.Owner, action.Comp), false);
            }

            return;
        }

        foreach (var actionUid in active.DisabledActions)
        {
            if (!TryComp(actionUid, out ActionComponent? action))
                continue;

            _actions.SetEnabled(new Entity<ActionComponent?>(actionUid, action), true);
        }

        active.DisabledActions.Clear();
    }

    public void ForceExit(EntityUid user)
    {
        if (!TryComp(user, out VentCrawlingComponent? active))
            return;

        ExitVent(user, active.SourceVent);
    }

    private void DisableTileMovement(EntityUid user)
    {
        RemComp<EntityTileMovementComponent>(user);

        if (!TryComp(user, out PhysicsComponent? physics))
            return;

        _physics.SetLinearVelocity(user, Vector2.Zero, body: physics);
        _physics.SetAngularVelocity(user, 0f, body: physics);
    }

    private void TryPlayVentCrawlSound(Entity<VentCrawlingComponent> ent, ref MoveEvent args)
    {
        if (!TryComp(ent, out ImperialVentCrawlerComponent? crawler) || crawler.MoveSound == null || crawler.MoveSoundInterval <= 0f)
            return;

        if (!args.OldPosition.TryDistance(EntityManager, args.NewPosition, out var distance) || distance <= 0f)
            return;

        var interval = crawler.MoveSoundInterval;

        ent.Comp.SoundDistance = distance > interval
            ? interval
            : ent.Comp.SoundDistance + distance;

        if (ent.Comp.SoundDistance < interval)
            return;

        ent.Comp.SoundDistance -= interval;
        _audio.PlayPvs(crawler.MoveSound, ent);
    }
}

