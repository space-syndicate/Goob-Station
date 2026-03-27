using Content.Shared.Imperial.EnergyCore;

namespace Content.Server.Imperial.EnergyCore.Components
{
    /// <summary>
    /// Система статуса ядра, определение ключевых параметров через Examine ивент.
    /// </summary>
    [RegisterComponent]
    public sealed partial class CoreStatusScreenComponent : Component
    {
        [ViewVariables(VVAccess.ReadOnly)]
        public CoreStatus ScreenStatus = CoreStatus.OFFLINE;

        [ViewVariables(VVAccess.ReadOnly)]
        public byte SpriteStatus = 0;

        // Ближайшее ядро
        [ViewVariables(VVAccess.ReadWrite)]
        public EntityUid? CheckedCore;

        // Ближайшее ядро: время данное на поиск
        [ViewVariables(VVAccess.ReadOnly)]
        public TimeSpan SearchTime = TimeSpan.FromSeconds(5);
    }
}
