using Content.Shared.Imperial.EnergyCore;

namespace Content.Server.Imperial.EnergyCore.Components
{
    /// <summary>
    /// Система статуса ядра, определение ключевых параметров через Examine ивент.
    /// </summary>
    [RegisterComponent]
    public sealed partial class CoreStatusScreenComponent : Component
    {
        [DataField, ViewVariables(VVAccess.ReadOnly)]
        public CoreStatus ScreenStatus = CoreStatus.OFFLINE;

        [DataField, ViewVariables(VVAccess.ReadOnly)]
        public byte SpriteStatus = 0;
    }
}
