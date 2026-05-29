using Content.Shared.Imperial.XxRaay.Components;
using Content.Shared.Interaction.Events;

namespace Content.Shared.Imperial.XxRaay.Systems;

public abstract class SharedImperialVentCrawlerSystem : EntitySystem
{
    [Dependency] private readonly SharedWormBloodDrinkSystem _wormBloodDrink = default!;
    [Dependency] private readonly SharedWormCorpsePossessionSystem _wormCorpsePossession = default!;
    [Dependency] private readonly SharedWormDoorHideSystem _wormDoorHide = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActiveVentCrawlingComponent, ComponentStartup>(OnVentCrawlStartup);
        SubscribeLocalEvent<ActiveVentCrawlingComponent, InteractionAttemptEvent>(OnInteractionAttempt);
        SubscribeLocalEvent<ActiveVentCrawlingComponent, UseAttemptEvent>(OnUseAttempt);
        SubscribeLocalEvent<ActiveVentCrawlingComponent, AttackAttemptEvent>(OnAttackAttempt);
    }

    private void OnVentCrawlStartup(Entity<ActiveVentCrawlingComponent> ent, ref ComponentStartup args)
    {
        _wormBloodDrink.StopDrinking(ent.Owner);
        _wormCorpsePossession.ForceExit(ent.Owner, forced: true);
        _wormDoorHide.ForceExit(ent.Owner);
    }

    private void OnInteractionAttempt(Entity<ActiveVentCrawlingComponent> ent, ref InteractionAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnUseAttempt(Entity<ActiveVentCrawlingComponent> ent, ref UseAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnAttackAttempt(Entity<ActiveVentCrawlingComponent> ent, ref AttackAttemptEvent args)
    {
        args.Cancel();
    }
}
