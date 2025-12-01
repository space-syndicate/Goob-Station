using Content.Shared.Actions;

namespace Content.Shared.Imperial.Vampire
{
    [Serializable]
    public sealed partial class VampireSleepEvent : InstantActionEvent
    {
        [DataField("costBlood")]
        public float CostBlood = 30;
    }
}
