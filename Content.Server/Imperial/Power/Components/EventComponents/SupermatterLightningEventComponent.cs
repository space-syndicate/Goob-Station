namespace Content.Server.Imperial.Power.Components.EventComponents;

[RegisterComponent]
public sealed partial class SupermatterLightningEventComponent : Component, ISupermatterEventComponent
{
    [DataField]
    public LocId? EventName { get; set; } = "supermatter-event-lightning-name";

    [DataField]
    public LocId EventDesc = "supermatter-event-lightning";

    [DataField]
    public TimeSpan? Duration;


    [DataField]
    public TimeSpan? EndTime;

    [DataField]
    public TimeSpan? EventAfterTime;


    [DataField]
    public int BoltCount = 1;

    [DataField]
    public float BoltRadius = 8f;


    [DataField]
    public TimeSpan? LightningInterval;

    [DataField]
    public TimeSpan NextLightningTime;
}
