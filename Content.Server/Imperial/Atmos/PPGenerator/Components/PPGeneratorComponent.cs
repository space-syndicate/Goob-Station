namespace Content.Server.Imperial.Power.Generation.PPG;

[RegisterComponent]
[Access(typeof(PPGSystem))]
public sealed partial class PPGeneratorComponent : Component
{
    [DataField]
    public float RampFactor = 1.05f;
    [DataField]
    public int PowerLevel;
    [DataField]
    public float Power = 0f;
    [DataField]
    public bool Active = false;
    [DataField]
    public float InitMissingGasTwo = 0f;
    [DataField]
    public float InitMissingGas = 0f;
    [DataField]
    public bool DeuteriumReactionActive = false;
    [DataField]
    public bool SecondaryGasActive = false;
    [DataField]
    public bool PrimaryGasActive = false;
    [DataField]
    public float Deuterium = 0f;
    [DataField]
    public float MinimumEnergy = 500f;
    [DataField]
    public float MinimumPressure = 10f;
    [DataField]
    public float MissingDeuteriumRate = 0.5f;
}
