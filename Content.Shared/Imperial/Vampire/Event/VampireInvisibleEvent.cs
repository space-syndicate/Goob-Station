using Content.Shared.Actions;

namespace Content.Shared.Imperial.Vampire
{
    public sealed partial class VampireInvisibleEvent : InstantActionEvent
    {
        /// <summary>
        /// сколько очков крови теряется в секунду при активной способности
        /// </summary>
        [DataField("costBlood")]
        public float CostBlood = 1;
    }
}
