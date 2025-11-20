using Content.Shared.Actions;
using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Imperial.Vampire
{
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    public sealed partial class VampireComponent : Component
    {
        [AutoNetworkedField]
        public bool IsActivated = false;

        [DataField]
        public string ClawId = "VampireClaw";

        [DataField]
        public float DamageBoost = 1.2f;

        [DataField]
        public float BoostSpeed = 1.2f;
        [DataField]
        public float BoostOnlySpeed = 2.5f;

        [DataField]
        public float AttackRateBoost = 1.15f;

        [DataField, AutoNetworkedField]
        public bool ItemIssued = false;

        [AutoNetworkedField]
        public float? OriginalDamageModifier = null;

        [AutoNetworkedField]
        public float? OriginalWalkSpeed = null;

        [AutoNetworkedField]
        public float? OriginalSprintSpeed = null;

        [AutoNetworkedField]
        public float? OriginalAttackRate = null;

        [AutoNetworkedField]
        public bool BuffBlocked;

        [DataField]
        public TimeSpan BuffBlockedUntil;

        [DataField]
        public TimeSpan UnBuffDuration = TimeSpan.FromSeconds(15);

        [DataField]
        public TimeSpan NextBloodDecay = TimeSpan.Zero;

        [DataField]
        public TimeSpan BloodDecayInterval = TimeSpan.FromSeconds(30);

        [DataField]
        public TimeSpan ShakingTime = TimeSpan.FromSeconds(5);

        [DataField]
        public float BloodDecayAmount = 5f;

        [DataField]
        public TimeSpan BuffDuration = TimeSpan.FromSeconds(10);

        [DataField, AutoNetworkedField]
        public bool TargetUser = false;

        [DataField]
        public float TeleportRadius = 105f;

        [DataField]
        public int SmokeRadius = 8;

        [DataField, AutoNetworkedField]
        public float CritThreshold = 100f;

        [DataField, AutoNetworkedField]
        public float BloodDamage;

        [DataField]
        public ProtoId<AlertPrototype> BloodAlert = "Blood";

        [DataField, AutoNetworkedField]
        public EntProtoId SmokePrototype = "Smoke";

        public EntProtoId GrimoreAction = "VampireGrimoireAction";
        public EntityUid? GrimoreActionEntity;
    }
}
