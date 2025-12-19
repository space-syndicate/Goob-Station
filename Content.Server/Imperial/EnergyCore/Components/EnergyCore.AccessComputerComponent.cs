using Content.Shared.Containers.ItemSlots;
using Content.Shared.Imperial.EnergyCore;
using Robust.Shared.Audio;

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
        [DataField("clickSound"), ViewVariables(VVAccess.ReadOnly)]
        public SoundSpecifier ClickSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");

        /// <summary>
        /// Попытка изменить реактивность/распад при предельный значениях
        /// </summary>
        [DataField("cancelSound"), ViewVariables(VVAccess.ReadOnly)]
        public SoundSpecifier CantSound = new SoundPathSpecifier("/Audio/Machines/custom_deny.ogg");

        /// <summary>
        /// Следующая попытка обновить UI
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadOnly)]
        public TimeSpan NextUIUpdate = default!;

        /// <summary>
        /// Апдейт UI
        /// </summary>

        public TimeSpan UpdateUIPeriod = TimeSpan.FromSeconds(1.0);

        /// <summary>
        /// Время требуемое для проверки кодов ядерной деавторизации
        /// </summary>
        [DataField("requiredTime"), ViewVariables(VVAccess.ReadWrite)]
        public TimeSpan TimeToCheck = TimeSpan.FromSeconds(10f);

        /// <summary>
        ///  Слот предмета
        /// </summary>
        [DataField("deCodeSlot"), ViewVariables(VVAccess.ReadWrite)]
        public ItemSlot DeCodeSlot = new();

        [DataField, ViewVariables(VVAccess.ReadOnly)]
        public float Reactivity = 30f;

        [DataField, ViewVariables(VVAccess.ReadOnly)]
        public float Halflife = 5f;

        [DataField, ViewVariables(VVAccess.ReadOnly)]
        public float FinalTempChangeCoef;

        [DataField, ViewVariables(VVAccess.ReadOnly)]
        public AutoSystemStatus AutoStatus = AutoSystemStatus.NONACTIVE;

        [DataField, ViewVariables(VVAccess.ReadOnly)]
        public TimeSpan Time = TimeSpan.Zero;

        [DataField, ViewVariables(VVAccess.ReadOnly)]
        public byte TerminalStatus = 1;

        [DataField, ViewVariables(VVAccess.ReadOnly)]
        public bool DeactivationCompleted = false;

        [DataField, ViewVariables(VVAccess.ReadOnly)]
        public bool TempRising = false;

        [DataField, ViewVariables(VVAccess.ReadOnly)]
        public bool SaveProtocolWasDeactivated = false;

        [DataField, ViewVariables(VVAccess.ReadOnly)]
        public byte ByteStatus = 1;

        #region Info from the Core

        [DataField, ViewVariables(VVAccess.ReadOnly)]
        public CoreStatus Status = CoreStatus.OFFLINE;

        [DataField, ViewVariables(VVAccess.ReadOnly)]
        public bool AutoSystem = false;

        [DataField, ViewVariables(VVAccess.ReadOnly)]
        public float CurrCoreTemp = 0;

        [DataField, ViewVariables(VVAccess.ReadOnly)]
        public float CurrentPowerSupply = 0;

        #endregion
    }
}
