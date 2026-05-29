using Content.Shared.Eye;
using Content.Shared.Imperial.XxRaay.Components;
using Content.Shared.Interaction.Events;

namespace Content.Shared.Imperial.XxRaay.Systems;

public abstract class SharedWormDoorHideSystem : EntitySystem
{
    [Dependency] private readonly SharedWormBloodDrinkSystem _wormBloodDrink = default!;
    [Dependency] private readonly SharedWormCorpsePossessionSystem _wormCorpsePossession = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActiveWormDoorHidingComponent, ComponentStartup>(OnHideStartup);
        SubscribeLocalEvent<ActiveWormDoorHidingComponent, ComponentShutdown>(OnHideShutdown);
        SubscribeLocalEvent<ActiveWormDoorHidingComponent, InteractionAttemptEvent>(OnInteractionAttempt);
        SubscribeLocalEvent<ActiveWormDoorHidingComponent, UseAttemptEvent>(OnUseAttempt);
        SubscribeLocalEvent<ActiveWormDoorHidingComponent, AttackAttemptEvent>(OnAttackAttempt);
        SubscribeLocalEvent<ActiveWormDoorHidingComponent, GetVisMaskEvent>(OnGetVisMask);
    }

    private void OnGetVisMask(Entity<ActiveWormDoorHidingComponent> ent, ref GetVisMaskEvent args)
    {
        if (!TryComp(ent.Owner, out WormDoorHiderComponent? hider) || !hider.EyeSeeDoorHideLayer)
            return;

        args.VisibilityMask |= hider.DoorHideVisibilityLayer;
    }

    public virtual void ForceExit(EntityUid worm)
    {
    }

    protected virtual void OnActiveDoorHideStarted(EntityUid worm)
    {
    }

    protected virtual void OnActiveDoorHideStopped(EntityUid worm)
    {
    }

    private void OnHideStartup(Entity<ActiveWormDoorHidingComponent> ent, ref ComponentStartup args)
    {
        _wormBloodDrink.StopDrinking(ent.Owner);
        _wormCorpsePossession.ForceExit(ent.Owner, forced: true);
        OnActiveDoorHideStarted(ent.Owner);
    }

    private void OnHideShutdown(Entity<ActiveWormDoorHidingComponent> ent, ref ComponentShutdown args)
    {
        OnActiveDoorHideStopped(ent.Owner);
    }

    private void OnInteractionAttempt(Entity<ActiveWormDoorHidingComponent> ent, ref InteractionAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnUseAttempt(Entity<ActiveWormDoorHidingComponent> ent, ref UseAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnAttackAttempt(Entity<ActiveWormDoorHidingComponent> ent, ref AttackAttemptEvent args)
    {
        args.Cancel();
    }
}
