using Content.Server.Imperial.Implants.Components;
using Content.Shared.Implants;
using Content.Shared.Nutrition.Components;
using Robust.Shared.Containers;

namespace Content.Server.Imperial.Implants.Systems;

public sealed class NutrimentPumpSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<NutrimentPumpComponent, ImplantImplantedEvent>(HandleImplant);
        SubscribeLocalEvent<NutrimentPumpComponent, EntGotRemovedFromContainerMessage>(HandleRemoval);
    }

    private void HandleImplant(EntityUid uid, NutrimentPumpComponent component, ImplantImplantedEvent args)
    {
        var target = args.Implanted;
        component.HadHunger = TryRemoveComponent<HungerComponent>(target);
        component.HadThirst = TryRemoveComponent<ThirstComponent>(target);
    }

    private void HandleRemoval(EntityUid uid, NutrimentPumpComponent component, EntGotRemovedFromContainerMessage args)
    {
        var target = args.Container.Owner;

        if (TerminatingOrDeleted(target))
            return;

        RestoreComponentIfNeeded<HungerComponent>(target, component.HadHunger);
        RestoreComponentIfNeeded<ThirstComponent>(target, component.HadThirst);
    }

    private bool TryRemoveComponent<T>(EntityUid target) where T : Component
    {
        if (!HasComp<T>(target))
            return false;

        RemCompDeferred<T>(target);
        return true;
    }

    private void RestoreComponentIfNeeded<T>(EntityUid target, bool shouldRestore) where T : Component, new()
    {
        if (shouldRestore)
            EnsureComp<T>(target);
    }
}
