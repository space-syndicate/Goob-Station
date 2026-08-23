using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;

namespace Content.Shared.Imperial.Medical;

/// <summary>
/// Used single-dose injectors keep <see cref="InjectorComponent"/> after emptying, which steals
/// AfterInteract from disposal units. Remove Injector once the solution is empty.
/// </summary>
public sealed class RemoveInjectorOnEmptySystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RemoveInjectorOnEmptyComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<RemoveInjectorOnEmptyComponent, SolutionContainerChangedEvent>(OnSolutionChanged);
    }

    private void OnMapInit(Entity<RemoveInjectorOnEmptyComponent> ent, ref MapInitEvent args)
    {
        TryRemoveIfEmpty(ent.Owner);
    }

    private void OnSolutionChanged(Entity<RemoveInjectorOnEmptyComponent> ent, ref SolutionContainerChangedEvent args)
    {
        if (!TryComp<InjectorComponent>(ent.Owner, out var injector))
            return;

        if (args.SolutionId != injector.SolutionName)
            return;

        TryRemoveIfEmpty(ent.Owner, injector);
    }

    private void TryRemoveIfEmpty(EntityUid uid, InjectorComponent? injector = null)
    {
        if (!Resolve(uid, ref injector))
            return;

        if (!_solutions.ResolveSolution(uid, injector.SolutionName, ref injector.Solution, out var solution)
            || solution.Volume > 0)
            return;

        RemCompDeferred<InjectorComponent>(uid);
    }
}
