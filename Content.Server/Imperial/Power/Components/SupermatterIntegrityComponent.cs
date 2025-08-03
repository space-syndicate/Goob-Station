using Robust.Shared.GameObjects;
using Content.Shared.Damage;

namespace Content.Server.Imperial.Power.Components
{
    [RegisterComponent]
    public sealed partial class SupermatterIntegrityComponent : Component
    {
        /// <summary>
        /// Текущая целостность кристалла
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite), DataField]
        public float Integrity = 100f;

        /// <summary>
        /// Максимальная целостность
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite), DataField]
        public float MaxIntegrity = 100f;

        /// <summary>
        /// Сколько урона наносится за тик при опасных условиях
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite), DataField]
        public DamageSpecifier TickDamage = new();

        /// <summary>
        /// Интервал между тиками урона
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite), DataField]
        public TimeSpan TickInterval = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Индивидуальный таймер для тиков урона
        /// </summary>
        [DataField]
        public TimeSpan TickAccumulator = TimeSpan.Zero;

        /// <summary>
        /// Минимальная целостность, при которой начинается катастрофа
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite), DataField]
        public float CatastropheThreshold = 0f;

        /// <summary>
        /// Флаги для стадий радио-предупреждений (ключ — порог процента)
        /// </summary>
        [DataField]
        public Dictionary<float, bool> WarningFlags = new()
        {
            { 0.9f, false },
            { 0.75f, false },
            { 0.5f, false },
            { 0.25f, false },
            { 0.10f, false }
        };

        /// <summary>
        /// Активна ли катастрофа
        /// </summary>
        [DataField]
        public bool CatastropheActive = false;

        /// <summary>
        /// Таймер катастрофы
        /// </summary>
        [DataField]
        public TimeSpan CatastropheTimer = TimeSpan.Zero;

        /// <summary>
        /// Тег для исцеления (например, "SupermatterHeal")
        /// </summary>
        [DataField]
        public string HealTag = "EmitterBolt";

        /// <summary>
        /// Количество здоровья, восстанавливаемое за один выстрел эмиттера
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite), DataField]
        public float EmitterHealAmount = 0.35f;

        // Описания состояния кристалла по проценту целостности
        [DataField]
        public Dictionary<float, LocId> IntegrityDescriptions = new()
        {
            { 0.95f, "supermatter-desc-pristine" },
            { 0.75f, "supermatter-desc-scratched" },
            { 0.5f,  "supermatter-desc-cracked" },
            { 0.25f, "supermatter-desc-badly-cracked" },
            { 0.0f,  "supermatter-desc-critical" }
        };
    }
}
