using Content.Server.Mind;
using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Shared.Chat;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Destructible;
using Content.Shared.GameTicking;
using Content.Shared.Imperial.XxRaay.Components;
using Content.Shared.Imperial.XxRaay.Systems;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.Imperial.XxRaay.Systems;

public sealed class WormEvolutionSystem : SharedWormEvolutionSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedMindSystem _sharedMind = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly WormBloodSystem _wormBlood = default!;
    [Dependency] private readonly WormCorpsePossessionSystem _corpsePossession = default!;
    [Dependency] private readonly VentCrawlerSystem _ventCrawler = default!;
    [Dependency] private readonly NPCSystem _npc = default!;
    [Dependency] private readonly HTNSystem _htn = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    private readonly HashSet<EntityUid> _completing = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WormCocoonComponent, DestructionEventArgs>(OnCocoonDestroyed);
        SubscribeLocalEvent<WormCocoonComponent, TransformSpeakerNameEvent>(OnCocoonTransformSpeakerName);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnCocoonTransformSpeakerName(Entity<WormCocoonComponent> ent, ref TransformSpeakerNameEvent args)
    {
        if (args.Sender != ent.Owner || !Exists(ent.Comp.Worm))
            return;

        args.VoiceName = Name(ent.Comp.Worm);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<WormCocoonComponent>();
        while (query.MoveNext(out var cocoonUid, out var cocoon))
        {
            if (_timing.CurTime < cocoon.EndTime)
                continue;

            CompleteEvolution(cocoonUid, cocoon);
        }
    }

    private void OnRoundRestart(RoundRestartCleanupEvent _)
    {
        _completing.Clear();
    }

    private void OnCocoonDestroyed(Entity<WormCocoonComponent> ent, ref DestructionEventArgs args)
    {
        if (_completing.Contains(ent.Owner))
            return;

        FailEvolution(ent.Owner, ent.Comp);
    }

    protected override void OnEvolutionStarted(EntityUid worm)
    {
        _ventCrawler.ForceExit(worm);
    }

    protected override void BeginEvolution(EntityUid worm, WormEvolverComponent evolver)
    {
        if (!CanEvolve((worm, evolver)))
            return;

        _wormBlood.ModifyBlood(worm, -evolver.BloodCost);

        var coords = Transform(worm).Coordinates;
        var cocoon = Spawn(evolver.CocoonProto, coords);

        var cocoonComp = EnsureComp<WormCocoonComponent>(cocoon);
        cocoonComp.Worm = worm;
        cocoonComp.ResultProto = evolver.ResultProto;
        cocoonComp.SourceProto = evolver.SourceProto;
        cocoonComp.EndTime = _timing.CurTime + TimeSpan.FromSeconds(evolver.EvolutionDuration);
        Dirty(cocoon, cocoonComp);

        var pausedMap = _corpsePossession.EnsurePausedMap();
        _transform.SetParent(worm, Transform(worm), pausedMap);

        var active = EnsureComp<ActiveWormEvolutionComponent>(worm);
        active.Cocoon = cocoon;

        if (_mind.TryGetMind(worm, out var mindId, out var mind) && mind.UserId != null)
        {
            active.PlayerControlled = true;
            _metaData.SetEntityName(cocoon, Name(worm));

            var cocoonXform = Transform(cocoon);
            _transform.SetLocalRotation(cocoon, Angle.Zero, cocoonXform);

            _mind.Visit(mindId, cocoon, mind);
        }
        else
            active.PlayerControlled = false;

        Dirty(worm, active);
    }

    private void CompleteEvolution(EntityUid cocoonUid, WormCocoonComponent cocoon)
    {
        if (_completing.Contains(cocoonUid))
            return;

        if (!Exists(cocoon.Worm) || !TryComp(cocoon.Worm, out ActiveWormEvolutionComponent? active))
        {
            if (Exists(cocoonUid))
                QueueDel(cocoonUid);
            return;
        }

        if (active.Cocoon != cocoonUid)
            return;

        _completing.Add(cocoonUid);

        try
        {
            var worm = cocoon.Worm;
            var coords = Transform(cocoonUid).Coordinates;
            var remainingBlood = TryComp<WormBloodComponent>(worm, out var blood) ? blood.Blood : 0;

            var newWorm = Spawn(cocoon.ResultProto, coords);

            if (remainingBlood > 0)
                _wormBlood.TryAddBlood(newWorm, remainingBlood);

            if (TryTransferPlayerMind(worm, cocoonUid, newWorm))
            {
                if (TryComp<HTNComponent>(newWorm, out var newHtn))
                    _npc.SleepNPC(newWorm, newHtn);
            }
            else if (TryComp<HTNComponent>(newWorm, out var newHtn))
            {
                _npc.WakeNPC(newWorm, newHtn);
                _htn.Replan(newHtn);
            }

            RemComp<ActiveWormEvolutionComponent>(worm);
            QueueDel(worm);
            QueueDel(cocoonUid);
        }
        finally
        {
            _completing.Remove(cocoonUid);
        }
    }

    private void FailEvolution(EntityUid cocoonUid, WormCocoonComponent cocoon)
    {
        if (_completing.Contains(cocoonUid))
            return;

        var worm = cocoon.Worm;
        var coords = Transform(cocoonUid).Coordinates;
        var playerControlled = Exists(worm)
            && TryComp(worm, out ActiveWormEvolutionComponent? active)
            && active.PlayerControlled;

        var corpse = SpawnDeadWorm(cocoon.SourceProto, coords);

        if (playerControlled)
            TryTransferPlayerMind(worm, cocoonUid, corpse);

        if (Exists(worm))
        {
            if (HasComp<ActiveWormEvolutionComponent>(worm))
                RemComp<ActiveWormEvolutionComponent>(worm);

            QueueDel(worm);
        }

        QueueDel(cocoonUid);
    }

    private bool TryTransferPlayerMind(EntityUid worm, EntityUid _, EntityUid target)
    {
        if (!_mind.TryGetMind(worm, out var mindId, out var mind) || mind.UserId == null)
            return false;

        _sharedMind.MakeSentient(target);
        _mind.TransferTo(mindId, target, ghostCheckOverride: true, mind: mind);
        return true;
    }

    private EntityUid SpawnDeadWorm(EntProtoId proto, EntityCoordinates coords)
    {
        var corpse = Spawn(proto, coords);
        _mobState.ChangeMobState(corpse, MobState.Dead);

        if (TryComp<MobThresholdsComponent>(corpse, out var thresholds))
        {
            var deadThreshold = _mobThreshold.GetThresholdForState(corpse, MobState.Dead, thresholds);
            var damage = new DamageSpecifier();
            damage.DamageDict["Blunt"] = deadThreshold;
            _damageable.TryChangeDamage(corpse, damage);
        }

        return corpse;
    }
}
