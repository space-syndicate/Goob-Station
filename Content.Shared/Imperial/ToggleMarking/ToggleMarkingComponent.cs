using Content.Shared.Humanoid.Markings;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Imperial.ToggleMarking;

/// <summary>
///     Allows toggling visibility of a specific category of humanoid markings,
///     such as ears or tail, typically via an action button.
/// </summary>
[RegisterComponent]
[NetworkedComponent]
public sealed partial class ToggleMarkingComponent : Component
{
    /// <summary>
    ///     The prototype ID of the action used to toggle the marking visibility.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Action;

    /// <summary>
    ///     The entity instance of the action. Assigned during map init or initialization.
    /// </summary>
    [DataField]
    public EntityUid? ActionEntity;

    /// <summary>
    ///     The category of markings this toggle controls (e.g. Tail, Ears).
    /// </summary>
    [DataField]
    public MarkingCategories Marking;

    /// <summary>
    ///     The localization string ID used for the toggle popup text.
    /// </summary>
    [DataField(required: true)]
    public LocId ToggleText;

    /// <summary>
    ///     Stores the original markings at the time the item is equipped,
    ///     used to restore state when the item is unequipped.
    /// </summary>
    public readonly List<Marking> MarkingsDefault = new();
}
