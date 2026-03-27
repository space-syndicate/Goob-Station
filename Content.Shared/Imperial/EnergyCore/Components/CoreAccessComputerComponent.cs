using Robust.Shared.Audio;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Imperial.EnergyCore;
using Content.Shared.Imperial.EnergyCore.Components;

namespace Content.Shared.Imperial.EnergyCore.Components
{
    /// <summary>
    /// Система терминала для управления и отключения протоколов безопасности энергетического ядра
    /// </summary>
    [RegisterComponent]
    public sealed partial class CoreAccessComputerComponent : Component
    {
        /// <summary>
        /// Ближайшее ядро
        /// </summary>
        [DataField]
        public EntityUid? ControledCore;

        // Ближайшее ядро: время данное на поиск
        [DataField]
        public TimeSpan SearchTime = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Звук при нажатии кнопки
        /// </summary>
        [DataField("clickSound")]
        public SoundSpecifier ClickSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");

        /// <summary>
        /// Попытка изменить реактивность/распад при предельный значениях
        /// </summary>
        [DataField("cancelSound")]
        public SoundSpecifier CantSound = new SoundPathSpecifier("/Audio/Machines/custom_deny.ogg");

        /// <summary>
        /// Следующая попытка обновить UI, с учетом UpdateUIPeriod и CurTime
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        public TimeSpan NextUIUpdate = default!;

        /// <summary>
        /// Задержка между обновами
        /// </summary>
        [DataField]
        public TimeSpan UpdateUIPeriod = TimeSpan.FromSeconds(0.2);

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

        [ViewVariables(VVAccess.ReadOnly)]
        public float Reactivity = 30f;

        [ViewVariables(VVAccess.ReadOnly)]
        public float Halflife = 5f;

        [ViewVariables(VVAccess.ReadOnly)]
        public float FinalTempChangeCoef;

        [ViewVariables(VVAccess.ReadOnly)]
        public AutoSystemStatus AutoStatus = AutoSystemStatus.NONACTIVE;

        [ViewVariables(VVAccess.ReadOnly)]
        public TimeSpan Time = TimeSpan.Zero;

        [ViewVariables(VVAccess.ReadOnly)]
        public byte TerminalStatus = 1;

        [ViewVariables(VVAccess.ReadOnly)]
        public bool DeactivationCompleted = false;

        [ViewVariables(VVAccess.ReadOnly)]
        public bool TempRising = false;

        [ViewVariables(VVAccess.ReadOnly)]
        public bool SaveProtocolWasDeactivated = false;

        //[ViewVariables(VVAccess.ReadOnly)]
        //public byte ByteStatus = 1;
        [ViewVariables(VVAccess.ReadOnly)]
        public CoreTempChangeLevel TempRiseTerminal = CoreTempChangeLevel.COOLING;

        #region Info from the Core

        [ViewVariables(VVAccess.ReadOnly)]
        public CoreStatus Status = CoreStatus.OFFLINE;

        [ViewVariables(VVAccess.ReadOnly)]
        public bool AutoSystem = false;

        [ViewVariables(VVAccess.ReadOnly)]
        public float CurrCoreTemp = 0;

        [ViewVariables(VVAccess.ReadOnly)]
        public float CurrentPowerSupply = 0;

        #endregion
    }
}
