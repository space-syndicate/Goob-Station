namespace Content.Shared.Imperial.JanitorsResponseTeam.Components;

[RegisterComponent]
public sealed partial class TrashComponent : Component
{
    public enum TrashSize
    {
        Small,
        Medium,
        Large
    }

    [DataField]
    public TrashSize Size = TrashSize.Small;
}
