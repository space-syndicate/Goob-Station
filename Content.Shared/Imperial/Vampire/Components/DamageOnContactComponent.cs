using Content.Shared.Damage;
using Content.Shared.FixedPoint;

namespace Content.Shared.Imperial.Vampire
{
    [RegisterComponent]
    public sealed partial class DamageOnContactComponent : Component
    {
        /// <summary>
        /// количество урона, а так же тип, которое будет нанесено при соприкосновении
        /// </summary>
        [DataField("damage")]
        public DamageSpecifier Damage = new DamageSpecifier
        {
            DamageDict = new Dictionary<string, FixedPoint2>
            {
                ["Slash"] = 15
            }
        };

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
