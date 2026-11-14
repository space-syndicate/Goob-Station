using Content.Server.Ghost;
using Content.Shared._BlackM.Elevator;
using Content.Shared.Administration;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;

namespace Content.Server._BlackM.Elevator;

public sealed class EvacuationObserverSystem : EntitySystem
{
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly GhostSystem _ghost = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BlackMElevatorArrivedEvent>(OnEvacuated);
        SubscribeNetworkEvent<EvacuationObserverRequestEvent>(OnObserverRequest);
    }

    private void OnEvacuated(BlackMElevatorArrivedEvent args)
    {
        if (args.ElevatorId != "evac" || !_mobState.IsAlive(args.Entity) ||
            !_mind.TryGetMind(args.Entity, out _, out _))
            return;

        var observer = EnsureComp<EvacuationObserverComponent>(args.Entity);
        observer.Body = args.Entity;
        observer.IsObserving = false;
        Dirty(args.Entity, observer);
    }

    private void OnObserverRequest(EvacuationObserverRequestEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } uid ||
            !TryComp<EvacuationObserverComponent>(uid, out var observer) ||
            HasComp<AdminFrozenComponent>(uid) || TerminatingOrDeleted(observer.Body) ||
            HasComp<AdminFrozenComponent>(observer.Body) ||
            !_mind.TryGetMind(args.SenderSession, out var mindId, out var mind) ||
            mind.OwnedEntity != observer.Body || mind.CurrentEntity != uid)
            return;

        if (msg.ReturnToBody)
        {
            if (observer.IsObserving && mind.VisitingEntity == uid)
                _mind.UnVisit(mindId, mind);
            return;
        }

        if (observer.IsObserving || uid != observer.Body || mind.VisitingEntity != null)
            return;

        // Visit a normal observer while keeping the survivor and evacuation credit intact.
        if (_ghost.SpawnGhost((mindId, mind), canReturn: true) is not { } ghost)
            return;

        var ghostObserver = EnsureComp<EvacuationObserverComponent>(ghost);
        ghostObserver.Body = observer.Body;
        ghostObserver.IsObserving = true;
        Dirty(ghost, ghostObserver);
    }
}
