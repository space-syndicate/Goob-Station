using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Imperial.Vampire
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class VampireTrapOnTriggerComponent : Component
    {
        /// <summary>
        /// количество урона, которое будет нанесено при соприкосновении
        /// </summary>
        [DataField("damage")]
        public int Damage = 20;

        /// <summary>
        /// тип урона, который будет нанесен при соприкосновении
        /// </summary>
        [DataField("damageType")]
        public string DamageType = "Slash";

        /// <summary>
        /// идентификатор приспособления, с которым объект должен столкнуться для получения урона
        /// </summary>
        [DataField]
        public string FixtureId = "fix";

        /// <summary>
        /// на сколько секунд жертва ослепнет при соприкосновении
        /// </summary>
        [DataField("blindingTime")]
        public TimeSpan BlindingTime = TimeSpan.FromSeconds(10);

        /// <summary>
        /// звук при соприкосновении
        /// </summary>
        [DataField("shadowTrapSound")]
        public SoundSpecifier ShadowTrapSound = new SoundPathSpecifier("/Audio/Effects/chopstickbreak.ogg")
        {
            Params = AudioParams.Default.WithVolume(5)
        };
    }
}
