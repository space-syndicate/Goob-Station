using Content.Shared.Actions;
using Content.Shared.Cloning;
using Robust.Shared.Prototypes;

namespace Content.Shared.Imperial.Vampire
{
    public sealed class VampireEnvelopeGhoulEvent : EntityEventArgs
    {
        public EntityUid Vampire { get; }
        public EntityUid Target { get; }

        public VampireEnvelopeGhoulEvent(EntityUid vampire, EntityUid target)
        {
            Vampire = vampire;
            Target = target;
        }
    }

    public sealed partial class VampireCloneEvent : InstantActionEvent
    {
        [DataField("costBlood")]
        public float CostBlood = 10;

        [DataField]
        public ProtoId<CloningSettingsPrototype> Settings = "BaseClone";
    }

    public sealed partial class VampireRushBloodEvent : InstantActionEvent
    {
        /// <summary>
        /// множитель базовой скорости движения при активации бафа
        /// </summary>
        [DataField]
        public float BoostSpeed = 2;

        [DataField("costBlood")]
        public float CostBlood = 30;
    }

    public sealed partial class VampireUnCuffEvent : InstantActionEvent
    {
        [DataField]
        public float BoostSpeed = 1.5f;

        [DataField("costBlood")]
        public float CostBlood = 30;
    }

    public sealed partial class VampireSwordEvent : InstantActionEvent
    { }

    public sealed partial class VampireSwordPlusEvent : InstantActionEvent
    { }

    public sealed partial class VampireGrimoireEvent : InstantActionEvent
    { }

    public sealed partial class VampireMessageForGhouls : InstantActionEvent
    { }

    public sealed partial class VampireSelectingSubgroupEvent : InstantActionEvent
    { }

    public sealed partial class VampireBatTransformEvent : InstantActionEvent
    {
        [DataField("costBlood")]
        public float CostBlood = 10;
    }

    public sealed partial class VampireBloodTheftEvent : InstantActionEvent
    {
        [DataField("costBlood")]
        public float CostBlood = 10;
    }

    public sealed partial class VampireBloodTransformEvent : InstantActionEvent
    {
        [DataField("costBlood")]
        public float CostBlood = 10;
    }

    public sealed partial class VampireInvisibleEvent : InstantActionEvent
    {
        /// <summary>
        /// сколько очков крови теряется в секунду при активной способности
        /// </summary>
        [DataField("costBlood")]
        public float CostBlood = 1;
    }

    public sealed partial class VampireNosferatyEvent : InstantActionEvent
    {
        [DataField("costBlood")]
        public float CostBlood = 30;
    }

    public sealed partial class VampireReconciliationEvent : InstantActionEvent
    {
        [DataField("costBlood")]
        public float CostBlood = 30;
    }

    public sealed partial class VampireRecoveryEvent : InstantActionEvent
    {
        [DataField("costBlood")]
        public float CostBlood = 30;
    }

    public sealed partial class VampireShadowTrapEvent : WorldTargetActionEvent
    {
        [DataField("costBlood")]
        public float CostBlood = 30;
    }

    public sealed partial class VampireSleepEvent : InstantActionEvent
    {
        [DataField("costBlood")]
        public float CostBlood = 30;
    }

    public sealed partial class VampireTeleportEvent : InstantActionEvent
    {
        [DataField("costBlood")]
        public float CostBlood = 30;
    }

    public sealed partial class VampireTurnEvent : InstantActionEvent
    {
        [DataField("costBlood")]
        public float CostBlood = 30;
    }
}
