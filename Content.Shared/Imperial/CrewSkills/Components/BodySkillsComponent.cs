using Robust.Shared.GameStates;

namespace Content.Shared.Imperial.CrewSkills;


/// <summary>
/// Skills will be tied to the entity's body; when the mind changes, the person will lose the skills that are tied to that body.
/// </summary>
[RegisterComponent, Access(typeof(SharedCrewSkillsSystem))]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BodySkillsComponent : BaseSkillsComponent;
