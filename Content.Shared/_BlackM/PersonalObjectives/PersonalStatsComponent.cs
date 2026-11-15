namespace Content.Shared._BlackM.PersonalObjectives;

[RegisterComponent]
public sealed partial class BlackMPersonalStatsComponent : Component
{
    [ViewVariables]
    public Dictionary<string, int> Counters = new();

    [ViewVariables]
    public List<EntityUid> Objectives = new();

    [ViewVariables]
    public string? JobId;
}
