namespace Content.Server.Imperial.EnergyCore.Components
{
    /// <summary>
    /// Генератор энергетического ядра.
    /// </summary>
    [RegisterComponent]
    public sealed partial class CoreGeneratorComponent : Component
    {
        // Ближайшее ядро
        [DataField]
        public EntityUid? NearestCore;

        // Ближайшее ядро: время данное на поиск
        [DataField]
        public TimeSpan SearchTime = TimeSpan.FromSeconds(5);

        [DataField, ViewVariables(VVAccess.ReadOnly)]
        public float EnergyOutput = 0f;

        [DataField("energyCoefficient")]
        public float EnergyCoef = 1.7f;
    }
}
