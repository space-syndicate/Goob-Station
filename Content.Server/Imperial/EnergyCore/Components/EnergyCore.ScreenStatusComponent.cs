using Content.Shared.Imperial.EnergyCore;

namespace Content.Server.Imperial.EnergyCore.Components
{
    /// <summary>
    /// Система статуса ядра, определение ключевых параметров через Examine ивент.
    /// </summary>
    [RegisterComponent]
    public sealed partial class CoreStatusScreenComponent : Component
    {
        [DataField]
        public CoreStatus ScreenStatus = CoreStatus.OFFLINE;

        [DataField]
        public byte SpriteStatus = 0;
    }
}
