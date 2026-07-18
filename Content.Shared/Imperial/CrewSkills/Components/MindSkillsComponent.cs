using Robust.Shared.GameStates;

namespace Content.Shared.Imperial.CrewSkills;


/// <summary>
/// Skills tied to the mind are not lost when the mind changes.
/// </summary>
[RegisterComponent, Access(typeof(SharedCrewSkillsSystem))]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MindSkillsComponent : BaseSkillsComponent;
