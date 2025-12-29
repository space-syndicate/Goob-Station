using Content.Shared.Containers.ItemSlots;
using Content.Shared.Imperial.EnergyCore;
using Content.Shared.Imperial.EnergyCore.Components;
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
        [DataField]
        public TimeSpan NextUIUpdate = default!;

        /// <summary>
        /// Задержка между обновами
        /// </summary>
        [DataField]
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
        public float Reactivity = 30f;

        [DataField]
        public float Halflife = 5f;

        [DataField]
        public float FinalTempChangeCoef;

        [DataField]
        public AutoSystemStatus AutoStatus = AutoSystemStatus.NONACTIVE;

        [DataField]
        public TimeSpan Time = TimeSpan.Zero;

        [DataField]
        public byte TerminalStatus = 1;

        [DataField]
        public bool DeactivationCompleted = false;

        [DataField]
        public bool TempRising = false;

        [DataField]
        public bool SaveProtocolWasDeactivated = false;

        [DataField]
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
