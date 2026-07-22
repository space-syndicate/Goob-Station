namespace Content.Server.Imperial.Power.Components.EventComponents;

[RegisterComponent]
public sealed partial class SupermatterRadiationEventComponent : Component, ISupermatterEventComponent
{
    [DataField]
    public LocId? EventName { get; set; } = "supermatter-event-radiation-name";

    [DataField]
    public LocId EventDesc = "supermatter-event-radiation";

    [DataField]
    public TimeSpan? Duration;


    [DataField]
    public TimeSpan? EndTime;

    [DataField]
    public TimeSpan? EventAfterTime;


    [DataField]
    public float DefaultIntensity = 4.5f;

    [DataField]
    public float? Intensity;
}
