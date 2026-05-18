using Content.Server.Imperial.XxRaay.Components;
using Content.Server.Mind;
using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.GameTicking;
using Content.Shared.Imperial.XxRaay.Components;
using Content.Shared.Imperial.XxRaay.Systems;
using Content.Shared.Maps;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.StatusIcon.Components;
using System.Numerics;
using Content.Shared.Rejuvenate;
using Robust.Server.GameObjects;
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
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly NPCSystem _npc = default!;
    [Dependency] private readonly HTNSystem _htn = default!;
    [Dependency] private readonly WormBloodDrinkSystem _wormBloodDrink = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    private readonly HashSet<EntityUid> _exitingPossession = new();

    public EntityUid? PausedMap { get; private set; }

    public EntityUid EnsurePausedMap()
    {
        if (PausedMap != null && Exists(PausedMap))
            return PausedMap.Value;

        var mapUid = _map.CreateMap();
        _metaData.SetEntityName(mapUid, "worm-corpse-paused-map");
        _map.SetPaused(mapUid, true);
        PausedMap = mapUid;
        return mapUid;
    }

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WormCorpseOccupiedComponent, MobStateChangedEvent>(OnCorpseMobStateChanged);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent _)
    {
        if (PausedMap == null || !Exists(PausedMap))
            return;

        Del(PausedMap.Value);
        PausedMap = null;
    }

    private void OnCorpseMobStateChanged(Entity<WormCorpseOccupiedComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState is not (MobState.Critical or MobState.Dead))
            return;

        if (_exitingPossession.Contains(ent.Comp.Worm))
            return;

        ExitPossession(ent.Comp.Worm, ent.Owner, forced: true);
    }

    protected override void TryPossess(EntityUid worm, EntityUid corpse, WormCorpseHostComponent host)
    {
        if (!CanPossess((worm, host), corpse) || HasComp<VentCrawlingComponent>(worm) || HasComp<ActiveWormDoorHidingComponent>(worm) || HasComp<ActiveWormEvolutionComponent>(worm) || HasComp<ActiveWormReproductionComponent>(worm))
            return;

        _wormBloodDrink.StopDrinking(worm);

        if (!_mind.TryGetMind(worm, out var mindId, out var mind))
            (mindId, mind) = _mind.CreateMind(null, Name(worm));

        _sharedMind.MakeSentient(corpse);
        EnsureComp<ActionsComponent>(corpse);

        _mobState.ChangeMobState(corpse, MobState.Alive);
        RaiseLocalEvent(corpse, new RejuvenateEvent());

        var pausedMap = EnsurePausedMap();
        _transform.SetParent(worm, Transform(worm), pausedMap);

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

        if (!_exitingPossession.Add(worm))
            return;

        try
        {
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

            var bleed = new DamageSpecifier();
            bleed.DamageDict["Bloodloss"] = host.ExitBleedDamage;
            _damageable.TryChangeDamage(corpse, bleed);

            if (host.ExitSound != null)
                _audio.PlayPvs(host.ExitSound, corpse);

            FinishExit(worm, corpse, active, host, forced);
        }
        finally
        {
            _exitingPossession.Remove(worm);
        }
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
        RemComp<ActiveWormCorpsePossessionComponent>(worm);

        if (!forced && host?.EnterActionEntity != null)
            _actions.SetCooldown(host.EnterActionEntity, TimeSpan.FromSeconds(host.EnterCooldown));
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
        var wormXform = Transform(worm);

        _transform.SetParent(worm, wormXform, coordinates.EntityId);
        _transform.SetCoordinates(worm, wormXform, coordinates);

        if (TryComp(worm, out PhysicsComponent? physics))
        {
            _physics.SetLinearVelocity(worm, Vector2.Zero, body: physics);
            _physics.SetAngularVelocity(worm, 0f, body: physics);
        }
    }

}
