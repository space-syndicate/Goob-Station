using Content.Shared.Actions;
using Content.Shared.Alert;
using Content.Shared.Polymorph;
using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Imperial.Vampire
{
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    public sealed partial class VampireComponent : Component
    {
        /// <summary>
        /// ID сущности когтя вампира
        /// </summary>
        [DataField]
        public string ClawId = "VampireClaw";

        /// <summary>
        /// множитель урона при активном бафе
        /// </summary>
        [DataField]
        public float DamageBoost = 1.2f;

        /// <summary>
        /// множитель базовой скорости движения при активации бафа
        /// </summary>
        [DataField]
        public float BoostSpeed = 1.2f;

        /// <summary>
        /// множитель скорости, применяемый только к ходьбе
        /// </summary>
        [DataField]
        public float BoostOnlySpeed = 2.5f;

        /// <summary>
        /// множитель скорости атаки при активном бафе
        /// </summary>
        [DataField]
        public float AttackRateBoost = 1.15f;

        /// <summary>
        /// был ли уже выдан коготь игроку
        /// </summary>
        [AutoNetworkedField]
        public bool ItemIssued = false;

        /// <summary>
        /// оригинальный множитель урона до бафа
        /// </summary>
        [AutoNetworkedField]
        public float? OriginalDamageModifier = null;

        /// <summary>
        /// оригинальная скорость ходьбы до бафа
        /// </summary>
        [AutoNetworkedField]
        public float? OriginalWalkSpeed = null;

        /// <summary>
        /// оригинальная скорость бега до бафа
        /// </summary>
        [AutoNetworkedField]
        public float? OriginalSprintSpeed = null;

        /// <summary>
        /// оригинальная скорость атаки до бафа
        /// </summary>
        [AutoNetworkedField]
        public float? OriginalAttackRate = null;

        /// <summary>
        /// блокировка бафа (не позволяет одновременно активировать несколько бафов)
        /// </summary>
        [AutoNetworkedField]
        public bool BuffBlocked;

        /// <summary>
        /// время, до которого баф заблокирован
        /// </summary>
        [AutoNetworkedField]
        public TimeSpan BuffBlockedUntil;

        /// <summary>
        /// время следующего тика потери крови
        /// </summary>
        [AutoNetworkedField]
        public TimeSpan NextBloodDecay = TimeSpan.Zero;

        /// <summary>
        /// интервал между тиками потери крови
        /// </summary>
        [DataField]
        public TimeSpan BloodDecayInterval = TimeSpan.FromSeconds(30);

        /// <summary>
        /// длительность эффекта дрожания после того, как крови становится меньше нуля
        /// </summary>
        [DataField]
        public TimeSpan ShakingTime = TimeSpan.FromSeconds(5);

        /// <summary>
        /// количество крови, теряемое за один тик
        /// </summary>
        [DataField]
        public float BloodDecayAmount = 1.5f;

        /// <summary>
        /// продолжительность действия бафа
        /// </summary>
        [DataField]
        public TimeSpan BuffDuration = TimeSpan.FromSeconds(10);

        /// <summary>
        /// определяет, телепортируется ли сам игрок
        /// </summary>
        [AutoNetworkedField]
        public bool TargetUser = false;

        /// <summary>
        /// радиус телепортации вампира
        /// </summary>
        [DataField]
        public float TeleportRadius = 105f;

        /// <summary>
        /// радиус визуального эффекта дыма при телепортации
        /// </summary>
        [DataField]
        public int SmokeRadius = 8;

        /// <summary>
        /// стартовое количество крови у вампира
        /// </summary>
        [AutoNetworkedField]
        public float CritThreshold = 100f;

        /// <summary>
        /// ID алерта для отображения состояния крови
        /// </summary>
        [DataField]
        public ProtoId<AlertPrototype> BloodAlert = "Blood";

        /// <summary>
        /// ID дыма, которай спавнится при телепортации
        /// </summary>
        [DataField, AutoNetworkedField]
        public EntProtoId SmokePrototype = "Smoke";

        public EntProtoId GrimoreAction = "VampireTestAction";
        public EntityUid? GrimoreActionEntity;
        public EntProtoId SelectingSubgroupAction = "VampireSelectingSubgroup";

        [AutoNetworkedField]
        public float BloodDamage;

        [AutoNetworkedField]
        public HashSet<EntityUid> Ghouls = new();

        [AutoNetworkedField]
        public int GhoulQuantity = 0;

        [AutoNetworkedField]
        public bool InvisibleIsActive = false;

        /// <summary>
        /// сколько очков крови теряется в секунду при активной способности
        /// </summary>
        [DataField("bloodLossDisguiseIsActive")]
        public float BloodLossDisguiseIsActive = 1;

        [AutoNetworkedField]
        public TimeSpan NextBloodDecayDisguise = TimeSpan.Zero;

        [AutoNetworkedField]
        public bool VampireIsBat = false;

        [AutoNetworkedField]
        public bool VampireIsBlood = false;

        [AutoNetworkedField]
        public TimeSpan NextBloodshed = TimeSpan.Zero;

        [AutoNetworkedField]
        public bool VampireIsInvisibility = false;

        [DataField]
        public TimeSpan BloodDecayIntervalInvisible = TimeSpan.FromSeconds(1);

        /// <summary>
        /// активна ли маскировка (игрок не может одновременно находиться в инвизе, быть летучей мышью/призраком)
        /// </summary>
        [AutoNetworkedField]
        public bool DisguiseIsActive = false;

        [AutoNetworkedField]
        public bool DoAfterShadowTrapIsActive = false;

        [AutoNetworkedField]
        public List<EntityUid> GrantedActions = new();

        [AutoNetworkedField]
        public int SelectedSubgroup = 0;

        [AutoNetworkedField]
        public TimeSpan ClawDurationActive;

        [DataField]
        public TimeSpan ClawDuration = TimeSpan.FromSeconds(4);

        /// <summary>
        /// сколько всего крови выпил вампир
        /// </summary>
        [AutoNetworkedField]
        public float TotalDrunk = 0;

        [AutoNetworkedField]
        public HashSet<int> UnlockedAbilityIndices = new();

        [AutoNetworkedField]
        public bool InvisibleCloneIsActive = false;

        /// <summary>
        /// выбрана ли подгруппа?
        /// </summary>
        [AutoNetworkedField]
        public bool DirectionSelected = false;

        [DataField]
        public ProtoId<FactionIconPrototype> StatusIcon = "VampireFaction";

        public float Radius = 2f;
    }
}
