namespace Content.Server.Imperial.KAKTYC.TitorClock;

[RegisterComponent]
[Access(typeof(TitorClockSystem))]
public sealed partial class TitorClockComponent : Component
{
    [DataField]
    public float TitorNumber = 0;
    [DataField]
    public float UniversalNumber = 1;
    [DataField]
    public float CoefficentNumber = 0.7f;
    [DataField]
    public float CoefficentNumberTwo = 1;
}
