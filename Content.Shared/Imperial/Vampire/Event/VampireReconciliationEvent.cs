using Content.Shared.Actions;

namespace Content.Shared.Imperial.Vampire
{
    public sealed partial class VampireReconciliationEvent : InstantActionEvent
    {
        [DataField("costBlood")]
        public float CostBlood = 30;
    }
}
