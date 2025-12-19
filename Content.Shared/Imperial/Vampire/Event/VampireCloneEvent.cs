using Content.Shared.Actions;
using Content.Shared.Cloning;
using Robust.Shared.Prototypes;

namespace Content.Shared.Imperial.Vampire
{
    public sealed partial class VampireCloneEvent : InstantActionEvent
    {
        [DataField("costBlood")]
        public float CostBlood = 10;

        [DataField]
        public ProtoId<CloningSettingsPrototype> Settings = "BaseClone";
    }
}
