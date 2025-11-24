using Content.Shared.Whitelist;

namespace Content.Shared.Imperial.Vampire
{
    [RegisterComponent]
    public sealed partial class DamageOnContactComponent : Component
    {
        /// <summary>
        /// количество урона, которое будет нанесено при соприкосновении
        /// </summary>
        [DataField]
        public int Damage = 15;

        /// <summary>
        /// приспособление, с которым объект должен столкнуться, чтобы быть оглушенным
        /// </summary>
        [DataField]
        public string FixtureId = "fix";

        /// <summary>
        /// продолжительность оглушения
        /// </summary>
        [DataField]
        public TimeSpan Duration = TimeSpan.FromSeconds(5);

        /// <summary>
        /// следует ли обновить примененное оглушение?
        /// </summary>
        [DataField]
        public bool Refresh = true;

        /// <summary>
        /// должен ли оглушенный субъект пытаться встать, когда нокдаун закончится?
        /// </summary>
        [DataField]
        public bool AutoStand = true;
    }
}
