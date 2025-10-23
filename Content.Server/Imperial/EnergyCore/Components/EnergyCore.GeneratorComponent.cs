namespace Content.Server.Imperial.EnergyCore.Components
{
    /// <summary>
    /// Система статуса ядра, определение ключевых параметров через Examine ивент.
    /// </summary>
    [RegisterComponent]
    public sealed partial class CoreGeneratorComponent : Component
    {
        [DataField]
        public float EnergyOutput = 0f;

        [DataField("energyCoefficient")]
        public float EnergyCoef = 1.7f;
    }
}
