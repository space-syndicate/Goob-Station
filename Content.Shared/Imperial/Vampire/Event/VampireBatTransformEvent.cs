using Content.Shared.Actions;

namespace Content.Shared.Imperial.Vampire
{
    public sealed partial class VampireBatTransformEvent : InstantActionEvent
    {
        [DataField("costBlood")]
        public float CostBlood = 10;
    }
}
