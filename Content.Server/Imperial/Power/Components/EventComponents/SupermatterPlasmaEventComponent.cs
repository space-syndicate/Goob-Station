namespace Content.Server.Imperial.Power.Components.EventComponents;

[RegisterComponent]
public sealed partial class SupermatterPlasmaEventComponent : Component, ISupermatterEventComponent
{
    [DataField]
    public LocId? EventName { get; set; } = "supermatter-event-plasma-name";

    [DataField]
    public LocId EventDesc = "supermatter-event-plasma";

    [DataField]
    public TimeSpan? Duration;


    [DataField]
    public TimeSpan? EndTime;

    [DataField]
    public TimeSpan? EventAfterTime;


    [DataField]
    public float? HotspotTemperature;

    [DataField]
    public float? HotspotVolume;


    [DataField]
    public float? MolesAmount;

    [DataField]
    public TimeSpan? PlasmaInterval;

    [DataField]
    public TimeSpan NextPlasmaTime;
}
