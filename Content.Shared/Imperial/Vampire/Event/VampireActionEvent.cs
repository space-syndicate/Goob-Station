using Content.Shared.Actions;
using Content.Shared.Cloning;
using Content.Shared.Polymorph;
using Robust.Shared.Audio;
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

    public sealed partial class VampireTentaclesEvent : WorldTargetActionEvent
    {
        /// <summary>
        /// ID создаваемого объекта
        /// </summary>
        [DataField]
        public EntProtoId EntityId = "EffectVampireSpawn";

        /// <summary>
        /// указания, определяющие, где будут появляться сущности
        /// </summary>
        [DataField]
        public List<Direction> OffsetDirections = new()
        {
            Direction.North,
            Direction.South,
            Direction.East,
            Direction.West,
        };

        /// <summary>
        /// сколько сущностей появится сверх первоначальной в целевом местоположении?
        /// </summary>
        [DataField("extraSpawns")]
        public int ExtraSpawns = 3;

        [DataField("costBlood")]
        public float CostBlood = 30;

        /// <summary>
        /// кд между призывами щупалец
        /// </summary>
        [DataField("cooldownTentacles")]
        public TimeSpan CooldownTentacles = TimeSpan.FromSeconds(20);
    }

    public sealed partial class VampireCloneEvent : InstantActionEvent
    {
        [DataField("costBlood")]
        public float CostBlood = 10;

        [DataField]
        public ProtoId<CloningSettingsPrototype> Settings = "BaseClone";

        /// <summary>
        /// на сколько секунд вампир получит невидимость, пока клон отвлекает экипаж
        /// </summary>
        [DataField("invisibilityCloneTime")]
        public TimeSpan InvisibilityCloneTime = TimeSpan.FromSeconds(5);
    }

    public sealed partial class VampireRushBloodEvent : InstantActionEvent
    {
        /// <summary>
        /// множитель базовой скорости движения при активации бафа
        /// </summary>
        [DataField("boostSpeed")]
        public float BoostSpeed = 2.5f;

        /// <summary>
        /// сколько будет действовать RushBlood
        /// </summary>
        [DataField("rushBloodTime")]
        public TimeSpan RushBloodTime = TimeSpan.FromSeconds(10);

        [DataField("costBlood")]
        public float CostBlood = 30;
    }

    public sealed partial class VampireUnCuffEvent : InstantActionEvent
    {
        /// <summary>
        /// множитель базовой скорости движения при активации бафа
        /// </summary>
        [DataField("unCufBoostSpeed")]
        public float UnCufBoostSpeed = 1.5f;

        /// <summary>
        /// сколько будет действовать ускорение после снятия наручников
        /// </summary>
        [DataField("unCuffBuffTime")]
        public TimeSpan UnCuffBuffTime = TimeSpan.FromSeconds(6);

        [DataField("costBlood")]
        public float CostBlood = 30;
    }

    public sealed partial class VampireBatTransformEvent : InstantActionEvent
    {
        [DataField("costBlood")]
        public float CostBlood = 10;

        /// <summary>
        /// на сколько секунд вампир станет летучей мышью
        /// </summary>
        [DataField("polymorphBatTime")]
        public static int PolymorphBatTime = 10;

        [DataField("batCount")]
        public int BatCount = 3;

        [DataField]
        public string BatID = "MobVampireBat";

        [DataField]
        public PolymorphConfiguration BatConfig = new PolymorphConfiguration()
        {
            Entity = "MobVampireBat",
            Duration = PolymorphBatTime,
            TransferName = true,
            TransferHumanoidAppearance = false,
            TransferDamage = true,
            Inventory = PolymorphInventoryChange.Transfer,
        };
    }

    public sealed partial class VampireBloodTheftEvent : InstantActionEvent
    {
        [DataField("damageGhoul")]
        public float DamageGhoul = 10f;
    }

    public sealed partial class VampireBloodTransformEvent : InstantActionEvent
    {
        [DataField("costBlood")]
        public float CostBlood = 10;

        /// <summary>
        /// на сколько секунд вампир станет кровью
        /// </summary>
        [DataField("bloodTime")]
        public TimeSpan BloodTime = TimeSpan.FromSeconds(4);
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
        /// <summary>
        /// множитель урона при активном бафе
        /// </summary>
        [DataField("damageBoost")]
        public float DamageBoost = 2;

        /// <summary>
        /// множитель скорости атаки при активном бафе
        /// </summary>
        [DataField("attackRateBoost")]
        public float AttackRateBoost = 1.5f;

        /// <summary>
        /// множитель базовой скорости движения при активации бафа
        /// </summary>
        [DataField("boostSpeed")]
        public float BoostSpeed = 1.5f;

        [DataField("costBlood")]
        public float CostBlood = 30;

        [DataField("nosferatyTime")]
        public TimeSpan NosferatyTime = TimeSpan.FromSeconds(25);
    }

    public sealed partial class VampireReconciliationEvent : InstantActionEvent
    {
        [DataField("costBlood")]
        public float CostBlood = 30;

        /// <summary>
        /// длительность doAfter перед сверком
        /// </summary>
        [DataField("doAfterBeforeReconciliation")]
        public TimeSpan DoAfterBeforeReconciliation = TimeSpan.FromSeconds(1);

        /// <summary>
        /// сколько урона нанесет сверк предмету
        /// </summary>
        [DataField("reconciliationDamageItem")]
        public float ReconciliationDamageItem = 80;

        /// <summary>
        /// сколько урона стаминой получит игрок, если будет в зоне действия сверка
        /// </summary>
        [DataField("reconciliationStaminaDamage")]
        public float ReconciliationStaminaDamage = 100;

        /// <summary>
        /// на сколько секунд сверк отправит человека в Knockdown
        /// </summary>
        [DataField("reconciliationKnockdownHuman")]
        public TimeSpan ReconciliationKnockdownHuman = TimeSpan.FromSeconds(3);

        /// <summary>
        /// тип урона, который нанесет сверк предмету
        /// </summary>
        [DataField("damageType")]
        public string DamageType = "Slash";

        /// <summary>
        /// ID прототипа вампирской вспышки
        /// </summary>
        [DataField]
        public string VampireFlashEffectID = "VampireFlashEffect";
    }

    public sealed partial class VampireRecoveryEvent : InstantActionEvent
    {
        [DataField("costBlood")]
        public float CostBlood = 30;

        /// <summary>
        /// реагент для лечения
        /// </summary>
        [DataField("reagent")]
        public string ReagentID = "Omnizine";


        /// <summary>
        /// количество реагента
        /// </summary>
        [DataField("dose")]
        public float Dose = 25;
    }

    public sealed partial class VampireBloodAnchorEvent : InstantActionEvent
    {
        [DataField("costBlood")]
        public float CostBlood = 30;

        [DataField("anchorCreateTime")]
        public TimeSpan AnchorCreateTime = TimeSpan.FromSeconds(5);

        [DataField("durationExistenceAnchor")]
        public TimeSpan DurationExistenceAnchor = TimeSpan.FromSeconds(10);

        [DataField("vampireAnchorId")]
        public string VampireAnchorId = "VampireBloodAnchor";
    }

    public sealed partial class VampireJerkEvent : InstantActionEvent
    {
        [DataField("costBlood")]
        public float CostBlood = 30;

        /// <summary>
        /// сколько урона получит обьект при контакте с вампиром
        /// </summary>
        [DataField("damageItemOnContact")]
        public int DamageItemOnContact = 200;

        /// <summary>
        /// на сколько секунд будет оглушен игрок при контакте с вампиром
        /// </summary>
        [DataField("knockdownDuration")]
        public TimeSpan KnockdownDuration = TimeSpan.FromSeconds(3);

        /// <summary>
        /// какой тип урона получит игрок при контакте с вампиром
        /// </summary>
        [DataField("damageType")]
        public string DamageType = "Slash";
    }

    public sealed partial class VampireShadowTrapEvent : WorldTargetActionEvent
    {
        [DataField("costBlood")]
        public float CostBlood = 30;

        /// <summary>
        /// длительность doAfter перед установкой капкана
        /// </summary>
        [DataField("doAfterBeforeShadowTrap")]
        public TimeSpan DoAfterBeforeShadowTrap = TimeSpan.FromSeconds(5);

        /// <summary>
        /// максимальная дальность установки капкана от игрока
        /// </summary>
        [DataField("radius")]
        public float Radius = 2;

        [DataField]
        public string VampireTrapID = "VampireTrap";
    }

    public sealed partial class VampireSleepEvent : InstantActionEvent
    {
        [DataField("costBlood")]
        public float CostBlood = 30;

        /// <summary>
        /// длительность doAfter перед усыплением
        /// </summary>
        [DataField("doAfterBeforeEuthanasia")]
        public TimeSpan DoAfterBeforeEuthanasia = TimeSpan.FromSeconds(5);

        /// <summary>
        /// длительность сна
        /// </summary>
        [DataField("sleepingTime")]
        public TimeSpan SleepingTime = TimeSpan.FromSeconds(30);
    }

    public sealed partial class VampireTeleportEvent : InstantActionEvent
    {
        [DataField("costBlood")]
        public float CostBlood = 30;

        /// <summary>
        /// радиус телепортации вампира
        /// </summary>
        [DataField]
        public float TeleportRadius = 105f;
    }

    public sealed partial class VampireTurnEvent : InstantActionEvent
    {
        [DataField("costBlood")]
        public float CostBlood = 30;

        /// <summary>
        /// необходимо иметь упырей для обращения
        /// </summary>
        [DataField("necessaryGhoulQuantity")]
        public int NecessaryGhoulQuantity = 25;
    }

    public sealed partial class VampireSwordEvent : InstantActionEvent
    {
        [DataField("cooldownSword")]
        public TimeSpan CooldownSword = TimeSpan.FromSeconds(0);
    }

    public sealed partial class VampireGrimoireEvent : InstantActionEvent
    { }

    public sealed partial class VampireSelectingSubgroupEvent : InstantActionEvent
    { }
}
