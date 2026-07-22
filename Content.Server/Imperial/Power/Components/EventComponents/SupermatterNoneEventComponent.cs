namespace Content.Server.Imperial.Power.Components.EventComponents;

[RegisterComponent]
public sealed partial class SupermatterNoneEventComponent : Component, ISupermatterEventComponent
{
    [DataField]
    public LocId? EventName { get; set; }

    [DataField]
    public LocId EventDesc = "supermatter-event-none";

    [DataField]
    public TimeSpan? Duration;


    [DataField]
    public TimeSpan? EndTime;

    [DataField]
    public TimeSpan? EventAfterTime;
}
