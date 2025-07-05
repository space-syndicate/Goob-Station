namespace Content.Shared.Imperial.JanitorsResponseTeam.Components;

[RegisterComponent]
public sealed partial class SolutionDynamicColorOfStampComponent : Component
{
    /// <summary>
    /// Changed color
    /// </summary>
    [DataField]
    public Color Color;

    /// <summary>
    /// Do I need to check who put the seal on (profession)
    /// </summary>
    [DataField]
    public bool CheckValidRole = false;

    /// <summary>
    /// What role (profession) should be stamped
    /// </summary>
    [DataField]
    public string RoleName = ""; //change it in the prototype

    /// <summary>
    /// False seal name
    /// </summary>
    [DataField]
    public string FalseStampedName = "stamp-component-stamped-name-default"; //change it in the prototype

    /// <summary>
    /// if the profession matches, then it prints like this:
    /// </summary>
    [DataField]
    public string TrueStampedName = "stamp-component-stamped-name-default"; //change it in the prototype
}
