using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.Imperial.CrewSkills;


/// <summary>
/// This component auto-added when mind with skill inserted into new body
/// Component allows watch skills
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ExaminedSkillsComponent : Component
{
    [DataField]
    public ResPath ExaminedIcon = new("/Textures/Imperial/CrewSkills/VerbIcons/verb-icon-book-open.png");

    [DataField]
    public LocId ExaminedMessage = "skill-examine-msg";
}
