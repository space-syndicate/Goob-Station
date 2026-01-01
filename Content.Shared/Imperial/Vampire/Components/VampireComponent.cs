using Content.Shared.Actions;
using Content.Shared.Alert;
using Content.Shared.Polymorph;
using Content.Shared.StatusIcon;
using Robust.Shared.Audio;
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
        public string ClawId = "VampireSword";

        /// <summary>
        /// множитель урона при активном бафе
        /// </summary>
        [DataField]
        public float DamageBoost = 2;

        /// <summary>
        /// множитель базовой скорости движения при активации бафа
        /// </summary>
        [DataField]
        public float BoostSpeed = 1.5f;

        /// <summary>
        /// множитель скорости, применяемый только к ходьбе
        /// </summary>
        [DataField]
        public float BoostOnlySpeed = 3;

        /// <summary>
        /// множитель скорости атаки при активном бафе
        /// </summary>
        [DataField]
        public float AttackRateBoost = 1.5f;

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
        /// определяет, телепортируется ли сам игрок
        /// </summary>
        [AutoNetworkedField]
        public bool TargetUser = false;

        /// <summary>
        /// радиус визуального эффекта дыма при телепортации
        /// </summary>
        [DataField]
        public int SmokeRadius = 8;

        /// <summary>
        /// стартовое количество крови у вампира
        /// </summary>
        public float CritThreshold = 100f;

        /// <summary>
        /// ID алерта для отображения состояния крови
        /// </summary>
        [DataField]
        public ProtoId<AlertPrototype> BloodAlert = "VampireBloodAlert";

        /// <summary>
        /// на сколько секунд выдается меч
        /// </summary>
        [DataField]
        public TimeSpan ClawDuration = TimeSpan.FromSeconds(30);

        /// <summary>
        /// количество выпитой крови за 1 тик
        /// </summary>
        [DataField("bloodPerTick")]
        public float BloodPerTick = 1;

        /// <summary>
        /// сколько урона нанесет сверк предмету
        /// </summary>
        [DataField("reconciliationDamageItem")]
        public float ReconciliationDamageItem = 40f;

        /// <summary>
        /// на сколько секунд сверк отправит человека в Knockdown
        /// </summary>
        [DataField("reconciliationKnockdownHuman")]
        public TimeSpan ReconciliationKnockdownHuman = TimeSpan.FromSeconds(3);

        /// <summary>
        /// кд между призывами катаны
        /// </summary>
        [DataField("cooldownSword")]
        public TimeSpan CooldownSword = TimeSpan.FromSeconds(30);

        /// <summary>
        /// сколько длится doAfter перед обращением игрока в упыря
        /// </summary>
        [DataField("conversionGhoulTime")]
        public TimeSpan ConversionGhoulTime = TimeSpan.FromSeconds(5);

        /// <summary>
        /// звук катаны (достать)
        /// </summary>
        [DataField("getSwordSound")]
        public SoundSpecifier GetSwordSound = new SoundPathSpecifier("/Audio/Effects/gib1.ogg")
        {
            Params = AudioParams.Default.WithVolume(5f)
        };

        /// <summary>
        /// звук катаны (убрать)
        /// </summary>
        [DataField("removeSwordSound")]
        public SoundSpecifier RemoveSwordSound = new SoundPathSpecifier("/Audio/Effects/gib2.ogg")
        {
            Params = AudioParams.Default.WithVolume(5f)
        };

        /// <summary>
        /// звук телепорта
        /// </summary>
        [DataField("teleportSound")]
        public SoundSpecifier TeleportSound = new SoundPathSpecifier("/Audio/Magic/blink.ogg")
        {
            Params = AudioParams.Default.WithVolume(5)
        };

        /// <summary>
        /// звук телепорта
        /// </summary>
        [DataField("drinkSound")]
        public SoundSpecifier DrinkSound = new SoundPathSpecifier("/Audio/Items/drink.ogg")
        {
            Params = AudioParams.Default.WithVolume(3)
        };

        [DataField]
        public ProtoId<AlertPrototype> BloodCounterAlert = "VampireCounterAlert";

        /// <summary>
        /// ID дыма, которай спавнится при телепортации
        /// </summary>
        [DataField, AutoNetworkedField]
        public EntProtoId SmokePrototype = "Smoke";

        /// <summary>
        /// максимальное число, которое находится в bloodcount.rsi
        /// </summary>
        [DataField("maxDrink")]
        public float MaxDrink = 1000;

        /// <summary>
        /// количество спрайтов в bloodcount.rsi. необходимо для расчетов SetBloodCounterAlert
        /// </summary>
        [DataField("numberSections")]
        public int NumberSections = 21;

        public EntProtoId GrimoreAction = "VampireTestAction";
        public EntityUid? GrimoreActionEntity;
        public EntProtoId SelectingSubgroupAction = "VampireSelectingSubgroupAction";

        [AutoNetworkedField]
        public float BloodDamage;

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

        [DataField]
        public TimeSpan BloodDecayIntervalInvisible = TimeSpan.FromSeconds(1);

        /// <summary>
        /// активна ли маскировка (игрок не может одновременно находиться в инвизе, быть летучей мышью/призраком)
        /// </summary>
        [AutoNetworkedField]
        public bool DisguiseIsActive = false;

        [AutoNetworkedField]
        public List<EntityUid> GrantedActions = new();

        [AutoNetworkedField]
        public int SelectedSubgroup = 0;

        [AutoNetworkedField]
        public TimeSpan ClawDurationActive;

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
        public ProtoId<FactionIconPrototype> StatusIcon = "VampireFactionAction";

        [DataField]
        public float Radius = 2f;

        /// <summary>
        /// базовые способности, которые выдаются при получении роли
        /// </summary>
        public static readonly List<EntProtoId> BaseAbilities = new()
        {
            "VampireSwordAction",
            "VampireBloodTheftAction",
            "VampireRecoveryAction",
            "VampireSleepAction"
        };

        [AutoNetworkedField]
        public EntityUid SleepUid;

        [AutoNetworkedField]
        public HashSet<EntityUid> Ghouls = new();

        [AutoNetworkedField]
        public float GhoulQuantity = 0;

        [AutoNetworkedField]
        public bool InvisibleIsActive = false;

        /// <summary>
        /// для OnInvisible
        /// </summary>
        [AutoNetworkedField]
        public bool AbilityInvisibleIsActive = false;

        [AutoNetworkedField]
        public EntityUid VampireUid;
    }
}
