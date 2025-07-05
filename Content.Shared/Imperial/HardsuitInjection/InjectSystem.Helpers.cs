using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Imperial.HardsuitInjection.Components;
using Content.Shared.Popups;

namespace Content.Shared.Imperial.HardsuitInjection.EntitySystems;

public sealed partial class InjectSystem
{
    /// <summary>
    /// Toggle EC on hardsuit
    /// </summary>
    /// <param name="uid">Hardsuit uid</param>
    /// <param name="user">The person who will be shown messages about the opening and closing of the EС</param>
    public void ToggleEC(EntityUid uid, EntityUid user)
    {
        if (!TryComp<InjectComponent>(uid, out var component)) return;
        if (!TryComp<ItemSlotsComponent>(uid, out var comp)) return;

        if (component.Container == null) return;

        component.Locked = !component.Locked;

        _itemSlotsSystem.SetLock(uid, component.ContainerId, component.Locked, comp);

        if (component.Locked)
        {
            _popupSystem.PopupEntity(Loc.GetString("hardsuitinjection-close"), user, user, PopupType.Medium);
            _sharedAdminLogSystem.Add(LogType.ForceFeed, $"{_entManager.ToPrettyString(user):user} closed EC of {_entManager.ToPrettyString(uid):wearer}");

            return;
        }

        _popupSystem.PopupEntity(Loc.GetString("hardsuitinjection-open"), user, user, PopupType.Medium);
        _sharedAdminLogSystem.Add(LogType.ForceFeed, $"{_entManager.ToPrettyString(user):user} opened EC of {_entManager.ToPrettyString(uid):wearer}");
    }

    /// <summary>
    /// Inject reagent from ampula from hardsuit
    /// </summary>
    /// <param name="uid">Hardsuit uid</param>
    /// <param name="performer">Initiator of injection (For admin log)</param>
    public void Inject(EntityUid uid, EntityUid performer)
    {
        if (!TryComp<InjectComponent>(uid, out var component)) return;
        var action = _actionsSystem.GetAction(component!.InjectionActionEntity);

        if (action == null) return;
        if (action.Value.Comp.AttachedEntity == null) return;
        if (TryComp<ItemSlotsComponent>(action.Value.Comp.AttachedEntity, out var itemslots)) return;

        var user = action.Value.Comp.AttachedEntity.Value;
        var beaker = _itemSlotsSystem.GetItemOrNull(uid, component.ContainerId, itemslots);

        if (beaker == null)
        {
            _popupSystem.PopupEntity(Loc.GetString("hardsuitinjection-nobeaker"), user, user);

            return;
        }

        var actualBeaker = beaker.Value;

        if (!_solutions.TryGetSolution(actualBeaker, "beaker", out var solution)) return;
        if (!_solutions.TryGetInjectableSolution(
            (user, Comp<InjectableSolutionComponent>(user), Comp<SolutionContainerManagerComponent>(user)),
            out var targetSolutionEntity,
            out var targetSolution
        )) return;

        if (solution.Value.Comp.Solution.Volume <= 0)
        {
            _popupSystem.PopupEntity(Loc.GetString("hardsuitinjection-empty"), user, user);

            return;
        }

        var transferAmount = FixedPoint2.Min(solution.Value.Comp.Solution.Volume, targetSolution.AvailableVolume);
        if (transferAmount <= 0)
        {
            _popupSystem.PopupEntity(Loc.GetString("hardsuitinjection-full"), user, user);

            return;
        }

        var ev = new UpdateECEvent(GetNetEntity(actualBeaker), solution.Value.Comp.Solution, transferAmount);
        RaiseLocalEvent(uid, ev);

        if (ev.RemovedReagentAmount == null) return;

        var removedSolution = ev.RemovedReagentAmount;
        if (!targetSolution.CanAddSolution(removedSolution)) return;

        if (performer == uid)
            _sharedAdminLogSystem.Add(LogType.ForceFeed, $"{_entManager.ToPrettyString(user):user} injected his ES into yourself with a solution {SharedSolutionContainerSystem.ToPrettyString(removedSolution):removedSolution}");
        else
            _sharedAdminLogSystem.Add(LogType.ForceFeed, $"{_entManager.ToPrettyString(user):user} ES injected with a solution {SharedSolutionContainerSystem.ToPrettyString(removedSolution):removedSolution}");

        _reactiveSystem.DoEntityReaction(user, removedSolution, ReactionMethod.Injection);
        _solutions.TryAddSolution((user, targetSolutionEntity.Value.Comp), removedSolution);

        _audio.PlayPvs(component.InjectSound, user);
        _popupSystem.PopupEntity(Loc.GetString("hypospray-component-feel-prick-message"), user, user);
    }

}
