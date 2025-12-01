using Content.Shared.Actions;

namespace Content.Shared.Imperial.Vampire
{
    public sealed partial class VampireRushBloodEvent : InstantActionEvent
    {
        /// <summary>
        /// множитель базовой скорости движения при активации бафа
        /// </summary>
        [DataField]
        public float BoostSpeed = 2.0f;

        [DataField("costBlood")]
        public float CostBlood = 30;
    }
}
