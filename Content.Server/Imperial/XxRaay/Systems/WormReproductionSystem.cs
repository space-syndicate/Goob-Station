using System.Numerics;
using Content.Server.Mind;
using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Shared.Chat;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
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

public sealed class WormReproductionSystem : SharedWormReproductionSystem
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

    private static readonly Vector2[] OffspringOffsets =
    [
        new(0.35f, 0),
        new(-0.35f, 0),
        new(0, 0.35f),
    ];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WormReproductionCocoonComponent, DestructionEventArgs>(OnCocoonDestroyed);
        SubscribeLocalEvent<WormReproductionCocoonComponent, TransformSpeakerNameEvent>(OnCocoonTransformSpeakerName);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnCocoonTransformSpeakerName(Entity<WormReproductionCocoonComponent> ent, ref TransformSpeakerNameEvent args)
    {
        if (args.Sender != ent.Owner || !Exists(ent.Comp.ParentWorm))
            return;

        args.VoiceName = Name(ent.Comp.ParentWorm);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<WormReproductionCocoonComponent>();
        while (query.MoveNext(out var cocoonUid, out var cocoon))
        {
            if (_timing.CurTime < cocoon.EndTime)
                continue;

            CompleteReproduction(cocoonUid, cocoon);
        }
    }

    private void OnRoundRestart(RoundRestartCleanupEvent _)
    {
        _completing.Clear();
    }

    private void OnCocoonDestroyed(Entity<WormReproductionCocoonComponent> ent, ref DestructionEventArgs args)
    {
        if (_completing.Contains(ent.Owner))
            return;

        FailReproduction(ent.Owner, ent.Comp);
    }

    protected override void OnReproductionStarted(EntityUid worm)
    {
        _ventCrawler.ForceExit(worm);
    }

    protected override void BeginReproduction(EntityUid worm, WormReproducerComponent reproducer)
    {
        if (!CanReproduce((worm, reproducer)))
            return;

        _wormBlood.ModifyBlood(worm, -reproducer.BloodCost);

        var coords = Transform(worm).Coordinates;
        var cocoon = Spawn(reproducer.CocoonProto, coords);

        var cocoonComp = EnsureComp<WormReproductionCocoonComponent>(cocoon);
        cocoonComp.ParentWorm = worm;
        cocoonComp.OffspringProto = reproducer.OffspringProto;
        cocoonComp.OffspringCount = reproducer.OffspringCount;
        cocoonComp.ParentResultProto = reproducer.ParentResultProto;
        cocoonComp.SourceProto = reproducer.SourceProto;
        cocoonComp.FailDeathDamageType = reproducer.FailDeathDamageType;
        cocoonComp.EndTime = _timing.CurTime + reproducer.ReproductionDuration;
        Dirty(cocoon, cocoonComp);

        var pausedMap = _corpsePossession.EnsurePausedMap();
        _transform.SetParent(worm, Transform(worm), pausedMap);

        var active = EnsureComp<ActiveWormReproductionComponent>(worm);
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

    private void CompleteReproduction(EntityUid cocoonUid, WormReproductionCocoonComponent cocoon)
    {
        if (_completing.Contains(cocoonUid))
            return;

        if (!Exists(cocoon.ParentWorm) || !TryComp(cocoon.ParentWorm, out ActiveWormReproductionComponent? active))
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
            var worm = cocoon.ParentWorm;
            var coords = Transform(cocoonUid).Coordinates;
            var remainingBlood = TryComp<WormBloodComponent>(worm, out var blood) ? blood.Blood : 0;

            for (var i = 0; i < cocoon.OffspringCount; i++)
            {
                var offset = i < OffspringOffsets.Length ? OffspringOffsets[i] : Vector2.Zero;
                var spawnCoords = coords.Offset(offset);
                var offspring = Spawn(cocoon.OffspringProto, spawnCoords);

                if (TryComp<HTNComponent>(offspring, out var offspringHtn))
                {
                    _npc.WakeNPC(offspring, offspringHtn);
                    _htn.Replan(offspringHtn);
                }
            }

            var demotedWorm = Spawn(cocoon.ParentResultProto, coords);

            if (remainingBlood > 0)
                _wormBlood.TryAddBlood(demotedWorm, remainingBlood);

            if (TryTransferPlayerMind(worm, cocoonUid, demotedWorm))
            {
                if (TryComp<HTNComponent>(demotedWorm, out var demotedHtn))
                    _npc.SleepNPC(demotedWorm, demotedHtn);
            }

            RemComp<ActiveWormReproductionComponent>(worm);
            QueueDel(worm);
            QueueDel(cocoonUid);
        }
        finally
        {
            _completing.Remove(cocoonUid);
        }
    }

    private void FailReproduction(EntityUid cocoonUid, WormReproductionCocoonComponent cocoon)
    {
        if (_completing.Contains(cocoonUid))
            return;

        var worm = cocoon.ParentWorm;
        var coords = Transform(cocoonUid).Coordinates;
        var playerControlled = Exists(worm)
            && TryComp(worm, out ActiveWormReproductionComponent? active)
            && active.PlayerControlled;

        var corpse = SpawnDeadWorm(cocoon.SourceProto, coords, cocoon.FailDeathDamageType);

        if (playerControlled)
            TryTransferPlayerMind(worm, cocoonUid, corpse);

        if (Exists(worm))
        {
            if (HasComp<ActiveWormReproductionComponent>(worm))
                RemComp<ActiveWormReproductionComponent>(worm);

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

    private EntityUid SpawnDeadWorm(EntProtoId proto, EntityCoordinates coords, ProtoId<DamageTypePrototype> lethalDamageType)
    {
        var corpse = Spawn(proto, coords);
        _mobState.ChangeMobState(corpse, MobState.Dead);

        if (TryComp<MobThresholdsComponent>(corpse, out var thresholds))
        {
            var deadThreshold = _mobThreshold.GetThresholdForState(corpse, MobState.Dead, thresholds);
            var damage = new DamageSpecifier();
            damage.DamageDict[lethalDamageType] = deadThreshold;
            _damageable.TryChangeDamage(corpse, damage);
        }

        return corpse;
    }
}
