using Content.Shared.Actions;

namespace Content.Shared.Imperial.Vampire
{
    [Serializable]
    public sealed partial class VampireShadowTrapEvent : WorldTargetActionEvent
    {
        [DataField("costBlood")]
        public float CostBlood = 30;
    }
}
