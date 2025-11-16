using Content.Shared.Containers.ItemSlots;
using Content.Shared.Imperial.EnergyCore;
using Robust.Shared.Audio;
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
        /// Звук при нажатии кнопки
        /// </summary>
        [DataField("clickSound")]
        [ViewVariables(VVAccess.ReadWrite)]
        public SoundSpecifier ClickSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");

        /// <summary>
        /// Попытка изменить реактивность/распад при предельный значениях
        /// </summary>
        [DataField("cancelSound")]
        [ViewVariables(VVAccess.ReadWrite)]
        public SoundSpecifier CantSound = new SoundPathSpecifier("/Audio/Machines/custom_deny.ogg");

        /// <summary>
        /// Следующая попытка обновить UI
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public TimeSpan NextUIUpdate = default!;

        /// <summary>
        /// Апдейт UI
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public TimeSpan UpdateUIPeriod = TimeSpan.FromSeconds(1.0);

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
        [ViewVariables(VVAccess.ReadWrite)]
        public float Reactivity = 30f;

        [DataField]
        [ViewVariables(VVAccess.ReadWrite)]
        public float Halflife = 5f;

        [DataField]
        [ViewVariables(VVAccess.ReadWrite)]
        public float FinalTempChangeCoef;

        [DataField]
        public AutoSystemStatus AutoStatus = AutoSystemStatus.NONACTIVE;

        [DataField]
        [ViewVariables(VVAccess.ReadWrite)]
        public TimeSpan Time = TimeSpan.Zero;

        [DataField]
        [ViewVariables(VVAccess.ReadWrite)]
        public byte TerminalStatus = 1;

        [DataField]
        [ViewVariables(VVAccess.ReadWrite)]
        public bool DeactivationCompleted = false;

        [DataField]
        [ViewVariables(VVAccess.ReadWrite)]
        public bool TempRising = false;

        [DataField]
        [ViewVariables(VVAccess.ReadWrite)]
        public bool SaveProtocolWasDeactivated = false;

        [DataField]
        [ViewVariables(VVAccess.ReadWrite)]
        public byte ByteStatus = 1;

        #region Info from the Core

        [DataField]
        public CoreStatus Status = CoreStatus.OFFLINE;

        [DataField]
        public bool AutoSystem = false;

        [DataField]
        public float CurrCoreTemp = 0;

        [DataField]
        public float CurrentPowerSupply = 0;

        #endregion
    }
}
