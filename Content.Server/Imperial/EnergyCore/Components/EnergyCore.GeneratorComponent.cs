namespace Content.Server.Imperial.EnergyCore.Components
{
    /// <summary>
    /// Генератор энергетического ядра.
    /// </summary>
    [RegisterComponent]
    public sealed partial class CoreGeneratorComponent : Component
    {
        [DataField, ViewVariables(VVAccess.ReadOnly)]
        public float EnergyOutput = 0f;

        [DataField("energyCoefficient")]
        public float EnergyCoef = 1.7f;
    }
}
