using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Timing;

namespace Content.Server.Imperial.EnergyCore.Components
{
    /// <summary>
    /// Система терминала для отключения протоколов безопасности
    /// </summary>
    [RegisterComponent]
    public sealed partial class CoreAccessComputerComponent : Component
    {
        /// <summary>
        /// Время требуемое для проверки кодов ядерной деавторизации
        /// </summary>
        [DataField("requiredTime")]
        public TimeSpan TimeToCheck = TimeSpan.FromSeconds(10f);

        /// <summary>
        ///  Слот предмета
        /// </summary>
        [DataField("deCodeSlot")]
        public ItemSlot DeCodeSlot = new();

        [DataField]
        public TimeSpan Time = TimeSpan.Zero;

        [DataField]
        public byte TerminalStatus = 1;

        [DataField]
        public bool DeactivationCompleted = false;

        [DataField]
        public bool SaveProtocolWasDeactivated = false;
    }
}
