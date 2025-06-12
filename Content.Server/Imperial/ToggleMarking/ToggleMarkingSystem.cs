using System.Linq;
using Content.Shared.Actions;
using Content.Shared.Clothing;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Imperial.ToggleMarking;
using Content.Shared.Popups;

namespace Content.Server.Imperial.ToggleMarking;

/// <summary>
///     System that handles toggling visibility of humanoid markings
///     (e.g. ears or tail) via actions bound to clothing items.
/// </summary>
public sealed class ToggleMarkingSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ToggleMarkingComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ToggleMarkingComponent, GetItemActionsEvent>(OnGetItemActions);
        SubscribeLocalEvent<ToggleMarkingComponent, ClothingGotUnequippedEvent>(OnGotUnequipped);

        SubscribeLocalEvent<ToggleMarkingComponent, ToggleMarkingEarsEvent>(OnToggleEars);
        SubscribeLocalEvent<ToggleMarkingComponent, ToggleMarkingTailEvent>(OnToggleTail);
    }

    /// <summary>
    ///     Restores the original markings when the clothing is unequipped.
    ///     Also resets the action's toggled state and clears saved defaults.
    /// </summary>
    private void OnGotUnequipped(Entity<ToggleMarkingComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        if (!TryComp<HumanoidAppearanceComponent>(args.Wearer, out var huAp))
            return;

        for (var i = 0; i < ent.Comp.MarkingsDefault.Count; i++)
        {
            huAp.MarkingSet.Replace(ent.Comp.Marking, i, ent.Comp.MarkingsDefault[i]);
        }

        Dirty(args.Wearer, huAp);

        ent.Comp.MarkingsDefault.Clear();
        _actions.SetToggled(ent.Comp.ActionEntity, false);
    }

    /// <summary>
    ///     Adds the toggle action to the clothing item, if not currently held in hand.
    /// </summary>
    private void OnGetItemActions(Entity<ToggleMarkingComponent> ent, ref GetItemActionsEvent args)
    {
        if (args.InHands)
            return;

        args.AddAction(ref ent.Comp.ActionEntity, ent.Comp.Action);
    }

    private void OnMapInit(Entity<ToggleMarkingComponent> ent, ref MapInitEvent args)
    {
        _actionContainer.EnsureAction(ent.Owner, ref ent.Comp.ActionEntity, ent.Comp.Action);
    }

    /// <summary>
    ///     Handles the toggle action for ear markings.
    /// </summary>
    private void OnToggleEars(Entity<ToggleMarkingComponent> ent, ref ToggleMarkingEarsEvent args)
    {
        if (!TryToggleMarking(ent, args.Performer, args.Action.Comp.Toggled))
            return;

        args.Handled = true;
    }

    /// <summary>
    ///     Handles the toggle action for tail markings.
    /// </summary>
    private void OnToggleTail(Entity<ToggleMarkingComponent> ent, ref ToggleMarkingTailEvent args)
    {
        if (!TryToggleMarking(ent, args.Performer, args.Action.Comp.Toggled))
            return;

        args.Handled = true;
    }

    /// <summary>
    ///     Toggles visibility of the specified category of markings for the given user.
    ///     Saves the original marking state if not already stored,
    ///     then inverts visibility of all markings in the category.
    /// </summary>
    /// <param name="ent">The clothing with the <see cref="ToggleMarkingComponent"/>.</param>
    /// <param name="user">The player performing the action.</param>
    /// <param name="actionEnabled">Current toggle state from the action component.</param>
    /// <returns>True if the marking was successfully toggled; otherwise false.</returns>
    private bool TryToggleMarking(Entity<ToggleMarkingComponent> ent, EntityUid user, bool actionEnabled)
    {
        if (!TryComp<HumanoidAppearanceComponent>(user, out var huAp))
            return false;

        if (!huAp.MarkingSet.Markings.TryGetValue(ent.Comp.Marking, out var markings))
            return false;

        foreach (var marking in markings)
        {
            // Save the original marking once for restoration on unequip
            if (ent.Comp.MarkingsDefault.All(m => m.MarkingId != marking.MarkingId))
            {
                var newMark = new Marking(marking.MarkingId, marking.MarkingColors)
                {
                    Visible = marking.Visible,
                };

                ent.Comp.MarkingsDefault.Add(newMark);
            }

            marking.Visible = !marking.Visible;
        }

        _popup.PopupEntity(
            Loc.GetString(ent.Comp.ToggleText, ("visible", actionEnabled)),
            user,
            user);

        _actions.SetToggled(ent.Comp.ActionEntity, !actionEnabled);

        Dirty(user, huAp);
        return true;
    }
}
