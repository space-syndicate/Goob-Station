using Robust.Shared.GameStates;

namespace Content.Shared.Imperial.CrewSkills;


/// <summary>
/// This component attempts to add a <see cref="MindSkillsComponent" /> to the mind of the entity this component is added to when it is initialized.
/// </summary>
[RegisterComponent, Access(typeof(SharedCrewSkillsSystem))]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CrewSkillsComponent : BaseSkillsComponent
{
    /// <summary>
    /// Should we overwrite the <see cref="MindSkillsComponent" /> component if it exists?
    /// </summary>
    [DataField]
    public bool OverrideMindSkills = false;

    /// <summary>
    /// Need for
    /// </summary>
    [ViewVariables]
    public bool IsFirstVisitedEntity = true;
}
