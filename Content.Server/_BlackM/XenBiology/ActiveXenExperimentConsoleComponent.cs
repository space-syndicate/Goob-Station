namespace Content.Server._BlackM.XenBiology;

[RegisterComponent, Access(typeof(XenExperimentConsoleSystem))]
public sealed partial class ActiveXenExperimentConsoleComponent : Component
{
    public float UiUpdateAccumulator;

    public float IncidentChance;

    public EntityUid? User;
}
