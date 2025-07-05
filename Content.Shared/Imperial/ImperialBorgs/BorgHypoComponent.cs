using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Imperial.ImperialBorgs
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class BorgHypoComponent : Component
    {
        [DataField("solutions")]
        public List<BorgHypoSolution> Solutions = new();

        public int CurrentIndex = 0;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool UiUpdateNeeded;

        [ViewVariables(VVAccess.ReadWrite)]
        public string CurrentReagentName = "бикаридин";

        [DataField]
        public EntProtoId Action = "ChangeReagent";

        [DataField]
        public EntityUid? ActionEntity;
    }

    [DataDefinition]
    [Serializable, NetSerializable]
    public sealed partial class ImperialBorgsReagent
    {
        [DataField("reagentId")]
        public string ReagentId = null!;

        [DataField("quantity")]
        public float Quantity = 1.0f;

        [DataField("sprite")]
        public ResPath? Sprite;
    }
}
