namespace Content.Server.Imperial.Power.Generation.PPG;

[RegisterComponent]
[Access(typeof(PPGSystem))]
public sealed partial class PPGeneratorComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("rampFactor")]
    public float RampFactor = 1.05f;
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("powerLevel")]
    public int PowerLevel;
    [DataField("power")]
    public float Power = 0f;
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("active")]
    public bool Active = false;
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("initMissingGasTwo")]
    public float InitMissingGasTwo = 0f;
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("initMissingGas")]
    public float InitMissingGas = 0f;
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("active1")]
    public bool Active1 = false;
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("active2")]
    public bool Active2 = false;
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("active3")]
    public bool Active3 = false;
    [DataField("Deuterium")]
    public float Deuterium = 0f;

}
