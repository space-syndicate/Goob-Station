using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.Vampire
{
    [RegisterComponent]
    public sealed partial class VampireTrapOnTriggerComponent : Component
    {
        /// <summary>
        /// количество урона, которое будет нанесено при соприкосновении
        /// </summary>
        [DataField("damage")]
        public int Damage = 20;

        /// <summary>
        /// приспособление, с которым объект должен столкнуться, чтобы быть оглушенным
        /// </summary>
        [DataField]
        public string FixtureId = "fix";
    }
}
