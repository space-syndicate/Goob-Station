namespace Content.Server.Imperial.EnergyCore.Components
{
    /// <summary>
    /// Система статуса ядра, определение ключевых параметров через Examine ивент.
    /// </summary>
    [RegisterComponent]
    public sealed partial class CoreStatusScreenComponent : Component
    {
        [DataField]
        public byte ScreenStatus = 1;
    }
}
