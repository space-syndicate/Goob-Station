using Content.Shared.Actions;
using Content.Shared.Alert;
using Content.Shared.Eye;
using Content.Shared.IdentityManagement;
using Content.Shared.Imperial.XxRaay.Components;
using Content.Shared.Imperial.XxRaay.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Imperial.XxRaay.Systems;

public sealed class WormCocoonObserveSystem : SharedWormCocoonObserveSystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private EntityQuery<ActiveWormCorpsePossessionComponent> _corpsePossessionQuery;
    private EntityQuery<ActiveWormEvolutionComponent> _evolutionQuery;
    private EntityQuery<ActiveWormReproductionComponent> _reproductionQuery;

    public override void Initialize()
    {
        base.Initialize();

        _corpsePossessionQuery = GetEntityQuery<ActiveWormCorpsePossessionComponent>();
        _evolutionQuery = GetEntityQuery<ActiveWormEvolutionComponent>();
        _reproductionQuery = GetEntityQuery<ActiveWormReproductionComponent>();
    }

    public void SetupPlayerCocoon(EntityUid cocoon, EntityUid worm, TimeSpan startTime, TimeSpan endTime, ProtoId<AlertPrototype> timerAlert)
    {
        var observer = EnsureComp<WormCocoonObserverComponent>(cocoon);
        observer.SourceWorm = worm;
        DirtyField(cocoon, observer, nameof(WormCocoonObserverComponent.SourceWorm));

        _actions.AddAction(cocoon, ref observer.ObserveActionEntity, observer.ObserveAction);
        DirtyField(cocoon, observer, nameof(WormCocoonObserverComponent.ObserveActionEntity));

        if (HasComp<AlertsComponent>(cocoon))
            _alerts.ShowAlert(cocoon, timerAlert, cooldown: (startTime, endTime), autoRemove: true, showCooldown: true);
    }

    public void CleanupCocoon(EntityUid cocoon, ProtoId<AlertPrototype>? timerAlert = null)
    {
        ClearObservation(cocoon);

        if (timerAlert is { } alert && HasComp<AlertsComponent>(cocoon))
            _alerts.ClearAlert(cocoon, alert);

        if (TryComp(cocoon, out WormCocoonObserverComponent? observer))
        {
            if (observer.ObserveActionEntity != null)
                _actions.RemoveAction(cocoon, observer.ObserveActionEntity);

            RemComp<WormCocoonObserverComponent>(cocoon);
        }
    }

    protected override void ObserveRandomWorm(Entity<WormCocoonObserverComponent> cocoon, EntityUid? performer)
    {
        var candidates = new List<EntityUid>();
        CollectObservableWorms(cocoon.Comp.SourceWorm, candidates);

        if (candidates.Count == 0)
        {
            ShowObservePopup(cocoon, performer, Loc.GetString("worm-cocoon-observe-fail-none"));
            return;
        }

        var target = candidates[_random.Next(candidates.Count)];
        SetObservationTarget(cocoon, target);

        var message = Loc.GetString("worm-cocoon-observe-start",
            ("target", Identity.Entity(target, EntityManager)));

        ShowObservePopup(cocoon, performer, message);
    }

    private void CollectObservableWorms(EntityUid excludeWorm, List<EntityUid> results)
    {
        var query = EntityQueryEnumerator<WormBloodComponent, MobStateComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out _, out var xform))
        {
            if (uid == excludeWorm)
                continue;

            if (_mobState.IsDead(uid))
                continue;

            if (_corpsePossessionQuery.HasComp(uid)
                || _evolutionQuery.HasComp(uid)
                || _reproductionQuery.HasComp(uid))
                continue;

            if (xform.MapUid is not { } mapUid || _map.IsPaused(mapUid))
                continue;

            results.Add(uid);
        }
    }

    private void SetObservationTarget(Entity<WormCocoonObserverComponent> cocoon, EntityUid target)
    {
        cocoon.Comp.ObservedEntity = target;

        if (TryComp(cocoon, out EyeComponent? eye))
            _eye.SetTarget(cocoon, target, eye);
    }

    private void ClearObservation(EntityUid cocoon)
    {
        if (!TryComp(cocoon, out WormCocoonObserverComponent? observer))
            return;

        observer.ObservedEntity = null;

        if (TryComp(cocoon, out EyeComponent? eye))
            _eye.SetTarget(cocoon, null, eye);
    }

    public void ClearTargetFromAllObservers(EntityUid target)
    {
        var query = EntityQueryEnumerator<WormCocoonObserverComponent>();
        while (query.MoveNext(out var cocoon, out var observer))
        {
            if (observer.ObservedEntity == target)
            {
                ClearObservation(cocoon);
                ObserveRandomWorm((cocoon, observer), null);
            }
        }
    }
}
