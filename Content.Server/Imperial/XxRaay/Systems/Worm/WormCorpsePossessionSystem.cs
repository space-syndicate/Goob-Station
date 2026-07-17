using Content.Server.Ghost;
using Content.Server.Imperial.XxRaay.Components;
using Content.Server.Mind;
using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Administration.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Imperial.XxRaay.Components;
using Content.Shared.Imperial.XxRaay.Systems;
using Content.Shared.Maps;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.StatusIcon.Components;
using System.Numerics;
using Content.Shared.Ghost;
using Content.Shared.Follower;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Server.Imperial.XxRaay.Systems;

public sealed class WormCorpsePossessionSystem : SharedWormCorpsePossessionSystem
{
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedMindSystem _sharedMind = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly NPCSystem _npc = default!;
    [Dependency] private readonly HTNSystem _htn = default!;
    [Dependency] private readonly WormBloodDrinkSystem _wormBloodDrink = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly ISharedPlayerManager _players = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly GhostSystem _ghosts = default!;
    [Dependency] private readonly RejuvenateSystem _rejuvenate = default!;
    [Dependency] private readonly FollowerSystem _follower = default!;
    [Dependency] private readonly WormCocoonObserveSystem _cocoonObserve = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WormCorpseHostComponent, MapInitEvent>(OnHostMapInit);
        SubscribeLocalEvent<WormCorpseOccupiedComponent, MobStateChangedEvent>(OnCorpseMobStateChanged);
    }

    private void OnHostMapInit(Entity<WormCorpseHostComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent, ref ent.Comp.EnterActionEntity, ent.Comp.EnterAction);
    }

    protected override void OnHostShutdown(Entity<WormCorpseHostComponent> ent)
    {
        if (ent.Comp.EnterActionEntity != null)
            _actions.RemoveAction(ent.Owner, ent.Comp.EnterActionEntity);
    }

    private void OnCorpseMobStateChanged(Entity<WormCorpseOccupiedComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState is not (MobState.Critical or MobState.Dead))
            return;

        if (TryComp(ent.Comp.Worm, out ActiveWormCorpsePossessionComponent? active) && active.Exiting)
            return;

        ExitPossession(ent.Comp.Worm, ent.Owner, forced: true);
    }

    protected override void TryPossess(EntityUid worm, EntityUid corpse, WormCorpseHostComponent host)
    {
        if (!CanPossess((worm, host), corpse) || HasComp<VentCrawlingComponent>(worm) || HasComp<ActiveWormDoorHidingComponent>(worm) || HasComp<ActiveWormEvolutionComponent>(worm) || HasComp<ActiveWormReproductionComponent>(worm))
            return;

        _wormBloodDrink.StopDrinking(worm);

        DetachPlayersFromCorpse(corpse, worm);

        if (!_mind.TryGetMind(worm, out var mindId, out var mind))
            (mindId, mind) = _mind.CreateMind(null, Name(worm));

        _sharedMind.MakeSentient(corpse);
        EnsureComp<ActionsComponent>(corpse);

        _mobState.ChangeMobState(corpse, MobState.Alive);
        ApplyPossessionHealth(corpse, host);

        HideWormBody(worm);

        var active = EnsureComp<ActiveWormCorpsePossessionComponent>(worm);
        active.Corpse = corpse;
        active.CorpseCoordinates = Transform(corpse).Coordinates;
        active.RelocatedHtn = false;
        active.CorpseHadHtn = false;
        active.PlayerControlled = mind.UserId != null;
        Dirty(worm, active);

        if (active.PlayerControlled)
        {
            if (TryComp<HTNComponent>(worm, out var wormHtn))
                _npc.SleepNPC(worm, wormHtn);
        }
        else
        {
            RelocateHtn(worm, corpse, active);
        }

        _mind.TransferTo(mindId, corpse, ghostCheckOverride: true, mind: mind);

        EntityUid? exitAction = null;
        _actions.AddAction(corpse, ref exitAction, host.ExitAction);

        var hadStatusIcon = HasComp<StatusIconComponent>(corpse);
        if (!hadStatusIcon)
            EnsureComp<StatusIconComponent>(corpse);

        var occupied = EnsureComp<WormCorpseOccupiedComponent>(corpse);
        occupied.Worm = worm;
        occupied.ExitActionEntity = exitAction;
        occupied.AddedStatusIcon = !hadStatusIcon;
        Dirty(corpse, occupied);
    }

    protected override void ExitPossession(EntityUid worm, EntityUid corpse, bool forced)
    {
        if (!TryComp(worm, out ActiveWormCorpsePossessionComponent? active))
            return;

        if (active.Exiting)
            return;

        active.Exiting = true;
        Dirty(worm, active);

        corpse = active.Corpse;

        if (TerminatingOrDeleted(corpse))
        {
            FinishExitWithoutCorpse(worm, active, forced);
            return;
        }

        if (!TryComp(worm, out WormCorpseHostComponent? host))
        {
            FinishExit(worm, corpse, active, host: null, forced);
            return;
        }

        RemoveExitAction(corpse);

        _damageable.TryChangeDamage(corpse, host.ExitBleedDamage);
        EnsureCorpseDeadAfterWormExit(corpse);

        if (host.ExitSound != null)
            _audio.PlayPvs(host.ExitSound, corpse);

        FinishExit(worm, corpse, active, host, forced);
    }

    private void ApplyPossessionHealth(EntityUid corpse, WormCorpseHostComponent host)
    {
        _rejuvenate.PerformRejuvenate(corpse);

        if (!TryComp<MobThresholdsComponent>(corpse, out var thresholds))
        {
            _mobState.ChangeMobState(corpse, MobState.Alive);
            return;
        }

        if (!_mobThreshold.TryGetThresholdForState(corpse, MobState.Critical, out var criticalThreshold, thresholds))
        {
            if (!_mobThreshold.TryGetThresholdForState(corpse, MobState.Dead, out var deadThreshold, thresholds))
            {
                _mobState.ChangeMobState(corpse, MobState.Alive);
                return;
            }
            criticalThreshold = deadThreshold;
        }

        var fraction = Math.Clamp(host.PossessMinHealthFraction, 0f, 1f);
        var targetDamage = criticalThreshold.Value * FixedPoint2.New(1f - fraction);

        if (targetDamage > FixedPoint2.Zero)
        {
            var damage = new DamageSpecifier();
            damage.DamageDict[host.PossessDamageType] = targetDamage;
            _damageable.TryChangeDamage(corpse, damage);
        }

        _mobState.ChangeMobState(corpse, MobState.Alive);
    }

    /// <summary>
    /// Sends the worm body to nullspace and pauses it.
    /// Nullspace itself is not paused, so the entity must be paused explicitly.
    /// </summary>
    public void HideWormBody(EntityUid worm)
    {
        _follower.StopAllFollowers(worm);
        _cocoonObserve.ClearTargetFromAllObservers(worm);
        _transform.DetachEntity(worm, Transform(worm));
        SetPaused(worm, true);
    }

    private void RemoveExitAction(EntityUid corpse)
    {
        if (!TryComp(corpse, out WormCorpseOccupiedComponent? occupied) || occupied.ExitActionEntity is not {} actionUid)
            return;

        if (TryComp(actionUid, out ActionComponent? action) && action.AttachedEntity == corpse)
            _actions.RemoveAction(corpse, actionUid);

        occupied.ExitActionEntity = null;
        Dirty(corpse, occupied);
    }

    private void FinishExitWithoutCorpse(
        EntityUid worm,
        ActiveWormCorpsePossessionComponent active,
        bool forced)
    {
        PlaceWormAtStoredCoordinates(worm, active.CorpseCoordinates);

        if (_mind.TryGetMind(active.Corpse, out var mindId, out var mind)
            || _mind.TryGetMind(worm, out mindId, out mind))
        {
            _mind.TransferTo(mindId, worm, ghostCheckOverride: true, mind: mind);
        }

        ApplyPostExitHtn(worm, active.Corpse, active);

        TryComp(worm, out WormCorpseHostComponent? host);
        CleanupPossession(worm, active.Corpse, active, host, forced);
    }

    private void FinishExit(
        EntityUid worm,
        EntityUid corpse,
        ActiveWormCorpsePossessionComponent active,
        WormCorpseHostComponent? host,
        bool forced)
    {
        PlaceWormAtCorpse(worm, corpse);

        if (_mind.TryGetMind(corpse, out var mindId, out var mind)
            || _mind.TryGetMind(worm, out mindId, out mind))
        {
            _mind.TransferTo(mindId, worm, ghostCheckOverride: true, mind: mind);
        }

        ApplyPostExitHtn(worm, corpse, active);

        CleanupPossession(worm, corpse, active, host, forced);
    }

    private void CleanupPossession(
        EntityUid worm,
        EntityUid corpse,
        ActiveWormCorpsePossessionComponent active,
        WormCorpseHostComponent? host,
        bool forced)
    {
        if (TryComp(corpse, out WormCorpseOccupiedComponent? occupied) && occupied.AddedStatusIcon)
            RemComp<StatusIconComponent>(corpse);

        RemComp<WormCorpseOccupiedComponent>(corpse);
        RestoreVictimPlayers(corpse);
        RemComp<ActiveWormCorpsePossessionComponent>(worm);

        if (!forced && host?.EnterActionEntity != null)
            _actions.SetCooldown(host.EnterActionEntity, host.EnterCooldown);
    }

    private void RelocateHtn(EntityUid worm, EntityUid corpse, ActiveWormCorpsePossessionComponent active)
    {
        if (!TryComp<HTNComponent>(worm, out var wormHtn))
            return;

        _npc.SleepNPC(worm, wormHtn);

        active.CorpseHadHtn = HasComp<HTNComponent>(corpse);
        active.RelocatedHtn = true;

        var corpseHtn = EnsureComp<HTNComponent>(corpse);
        corpseHtn.RootTask = wormHtn.RootTask;
        corpseHtn.Blackboard.SetValue(NPCBlackboard.Owner, corpse);
        _npc.WakeNPC(corpse, corpseHtn);
        _htn.Replan(corpseHtn);

        Dirty(worm, active);
    }

    private void ApplyPostExitHtn(EntityUid worm, EntityUid corpse, ActiveWormCorpsePossessionComponent active)
    {
        if (active.PlayerControlled)
        {
            if (TryComp<HTNComponent>(worm, out var wormHtn))
                _npc.SleepNPC(worm, wormHtn);

            return;
        }

        if (!active.RelocatedHtn)
            return;

        if (TryComp<HTNComponent>(corpse, out var corpseHtn))
        {
            _npc.SleepNPC(corpse, corpseHtn);

            if (!active.CorpseHadHtn)
                RemComp<HTNComponent>(corpse);
        }

        if (TryComp<HTNComponent>(worm, out var wormHtnNpc))
        {
            _npc.WakeNPC(worm, wormHtnNpc);
            _htn.Replan(wormHtnNpc);
        }
    }

    private void PlaceWormAtCorpse(EntityUid worm, EntityUid corpse)
    {
        PlaceWormAtStoredCoordinates(worm, Transform(corpse).Coordinates);
    }

    private void PlaceWormAtStoredCoordinates(EntityUid worm, EntityCoordinates coordinates)
    {
        SetPaused(worm, false);

        var wormXform = Transform(worm);

        _transform.SetParent(worm, wormXform, coordinates.EntityId);
        _transform.SetCoordinates(worm, wormXform, coordinates);

        if (TryComp(worm, out PhysicsComponent? physics))
        {
            _physics.SetLinearVelocity(worm, Vector2.Zero, body: physics);
            _physics.SetAngularVelocity(worm, 0f, body: physics);
        }
    }

    private void DetachPlayersFromCorpse(EntityUid corpse, EntityUid worm)
    {
        var netCorpse = GetNetEntity(corpse);

        if (_mind.TryGetMind(corpse, out var corpseMindId, out var corpseMind)
            && corpseMind.UserId != null
            && (!_mind.TryGetMind(worm, out var wormMindId, out _) || corpseMindId != wormMindId))
        {
            if (corpseMind.OwnedEntity == corpse
                && corpseMind.VisitingEntity is { } visiting
                && HasComp<GhostComponent>(visiting))
            {
                if (_players.TryGetSessionById(corpseMind.UserId.Value, out var session)
                    && session.AttachedEntity == corpse)
                    _players.SetAttachedEntity(session, visiting);

                if (TryComp(visiting, out GhostComponent? visitingGhost))
                    _ghosts.SetCanReturnToBody((visiting, visitingGhost), false);
            }
            else
            {
                _ghosts.OnGhostAttempt(corpseMindId, canReturnGlobal: false, forced: true, mind: corpseMind);
            }
        }

        if (TryComp<ActorComponent>(corpse, out var actor)
            && actor.PlayerSession.AttachedEntity == corpse)
        {
            var detachSession = true;

            if (TryComp(worm, out MindContainerComponent? wormContainer)
                && wormContainer.Mind is { } possessedWormMindId
                && _mind.TryGetMind(possessedWormMindId, out _, out var possessedWormMind)
                && possessedWormMind.UserId == actor.PlayerSession.UserId)
            {
                detachSession = false;
            }

            if (detachSession)
                _players.SetAttachedEntity(actor.PlayerSession, null);
        }

        var query = EntityQueryEnumerator<MindComponent>();
        while (query.MoveNext(out var mindId, out var mind))
        {
            if (mind.UserId == null || mind.OriginalOwnedEntity != netCorpse)
                continue;

            if (_players.TryGetSessionById(mind.UserId.Value, out var session) && session.AttachedEntity == corpse)
            {
                if (TryGetPlayerGhost(mind, session, out var ghostUid))
                    _players.SetAttachedEntity(session, ghostUid);
                else
                    _players.SetAttachedEntity(session, null);
            }

            if (TryGetPlayerGhost(mind, null, out var ghost) && TryComp(ghost, out GhostComponent? ghostComp))
                _ghosts.SetCanReturnToBody((ghost, ghostComp), false);
        }
    }

    private void RestoreVictimPlayers(EntityUid corpse)
    {
        var netCorpse = GetNetEntity(corpse);
        var canReturn = _mobState.IsDead(corpse);

        var query = EntityQueryEnumerator<MindComponent>();
        while (query.MoveNext(out var mindId, out var mind))
        {
            if (mind.UserId == null || mind.OriginalOwnedEntity != netCorpse)
                continue;

            if (!TryGetPlayerGhost(mind, _players.TryGetSessionById(mind.UserId.Value, out var session) ? session : null, out var ghostUid)
                || !TryComp(ghostUid, out GhostComponent? ghost))
                continue;

            ReestablishGhostVisit(mindId, mind, corpse);

            _ghosts.SetCanReturnToBody((ghostUid, ghost), canReturn);
        }
    }

    /// <summary>
    /// Возврат в тело работает через Visit: OwnedEntity = труп, VisitingEntity = призрак, UnVisit переносит сессию на труп.
    /// </summary>
    private void ReestablishGhostVisit(EntityUid mindId, MindComponent mind, EntityUid corpse)
    {
        if (!Exists(corpse) || !TryGetPlayerGhost(mind, null, out var ghostUid))
            return;

        if (mind.OwnedEntity != corpse)
            _mind.TransferTo(mindId, corpse, ghostCheckOverride: true, mind: mind);

        if (mind.VisitingEntity == ghostUid)
            return;

        if (mind.VisitingEntity != null)
            _mind.UnVisit(mindId, mind);

        if (mind.VisitingEntity != ghostUid)
            _mind.Visit(mindId, ghostUid, mind);
    }

    private bool TryGetPlayerGhost(MindComponent mind, ICommonSession? session, out EntityUid ghostUid)
    {
        ghostUid = default;

        if (mind.VisitingEntity != null && HasComp<GhostComponent>(mind.VisitingEntity))
        {
            ghostUid = mind.VisitingEntity.Value;
            return true;
        }

        if (mind.OwnedEntity != null && HasComp<GhostComponent>(mind.OwnedEntity))
        {
            ghostUid = mind.OwnedEntity.Value;
            return true;
        }

        if (session?.AttachedEntity is { } attached && HasComp<GhostComponent>(attached))
        {
            ghostUid = attached;
            return true;
        }

        return false;
    }

    private void EnsureCorpseDeadAfterWormExit(EntityUid corpse)
    {
        if (_mobState.IsDead(corpse))
            return;

        _mobState.ChangeMobState(corpse, MobState.Dead);
    }

}
