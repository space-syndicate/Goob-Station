using Content.Shared.Body.Systems;
using Content.Shared.Gravity;
using Content.Shared.Movement.Systems;
using Content.Shared.Standing;
using Content.Shared.Stunnable;

namespace Content.Shared.Imperial.Traits;

public abstract partial class SharedImperialLegsParalyzedSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifierSystem = default!;
    [Dependency] private readonly StandingStateSystem _standingState = default!;
    [Dependency] private readonly SharedBodySystem _bodySystem = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ImperialLegsParalyzedComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ImperialLegsParalyzedComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ImperialLegsParalyzedComponent, StandUpAttemptEvent>(OnStandUpAttempt);
        SubscribeLocalEvent<ImperialLegsParalyzedComponent, WeightlessnessChangedEvent>(OnWeightlessnessChanged);
    }

    private void OnStartup(EntityUid uid, ImperialLegsParalyzedComponent component, ComponentStartup args)
    {
        component.AddedKnockdown = _stun.TryCrawling(uid, autoStand: false);
        _movementSpeedModifierSystem.ChangeBaseSpeed(
            uid,
            component.CrawlSpeed,
            component.CrawlSpeed,
            component.CrawlAcceleration
        );
    }

    private void OnShutdown(EntityUid uid, ImperialLegsParalyzedComponent component, ComponentShutdown args)
    {
        if (component.AddedKnockdown && TryComp<KnockedDownComponent>(uid, out var knockedDown))
        {
            _stun.CancelKnockdownDoAfter((uid, knockedDown));
            RemComp<KnockedDownComponent>(uid);
        }

        _bodySystem.UpdateMovementSpeed(uid);
    }

    private void OnWeightlessnessChanged(EntityUid uid, ImperialLegsParalyzedComponent component, WeightlessnessChangedEvent args)
    {
        if (args.Weightless)
        {
            _bodySystem.UpdateMovementSpeed(uid);
            _standingState.Down(uid);
        }
        else
        {
            _movementSpeedModifierSystem.ChangeBaseSpeed(
                uid,
                component.CrawlSpeed,
                component.CrawlSpeed,
                component.CrawlAcceleration
            );
        }
    }

    private void OnStandUpAttempt(EntityUid uid, ImperialLegsParalyzedComponent component, ref StandUpAttemptEvent args)
    {
        args.Cancelled = true;

        if (TryComp<KnockedDownComponent>(uid, out var knockedDown))
        {
            _stun.SetAutoStand((uid, knockedDown), false);
        }
    }
}
