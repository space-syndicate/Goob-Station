using Content.Shared.Actions;
using Robust.Shared.Prototypes;

namespace Content.Shared.Imperial.Vampire
{
    public sealed partial class VampireTurnEvent : InstantActionEvent
    {
        [DataField("costBlood")]
        public float CostBlood = 30;
    }
}
