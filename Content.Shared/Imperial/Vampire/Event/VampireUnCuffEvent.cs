using Content.Shared.Actions;

namespace Content.Shared.Imperial.Vampire
{
    [Serializable]
    public sealed partial class VampireUnCuffEvent : InstantActionEvent
    {
        [DataField]
        public float BoostSpeed = 1.5f;

        [DataField("costBlood")]
        public float CostBlood = 30;
    }
}
