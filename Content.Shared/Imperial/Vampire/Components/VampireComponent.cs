using Content.Shared.Alert;
using Content.Shared.StatusIcon;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared.Imperial.Vampire
{
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    public sealed partial class VampireComponent : Component
    {
        /// <summary>
        /// ID сущности когтя вампира
        /// </summary>
        [DataField("swordId")]
        public string SwordId = "VampireSword";

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
        /// кд между призывами катаны
        /// </summary>
        [AutoNetworkedField]
        public TimeSpan CooldownSword;

        /// <summary>
        /// кд между созданиями кровавого якоря
        /// </summary>
        [DataField("cooldownBloodAnchor")]
        public TimeSpan CooldownBloodAnchor = TimeSpan.FromSeconds(130);

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
        /// звук питья крови
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

        /// <summary>
        /// количество спрайтов в bleed.rsi. необходимо для расчетов VampireBloodAlert
        /// </summary>
        [DataField]
        public int NumberBloodSections = 10;

        /// <summary>
        /// раз во сколько секунд будет увеличиваться BloodDamage
        /// </summary>
        [DataField("bloodDecayIntervalInvisible")]
        public TimeSpan BloodDecayIntervalInvisible = TimeSpan.FromSeconds(1);

        /// <summary>
        /// сколько очков крови теряется в секунду при активной способности
        /// </summary>
        [DataField("bloodLossDisguiseIsActive")]
        public float BloodLossDisguiseIsActive = 1;

        [DataField]
        public string VampirePuddleID = "VampirePuddle";

        [DataField]
        public ProtoId<FactionIconPrototype> StatusIcon = "VampireFactionAction";

        [DataField]
        public string VampireRadioID = "VampireRadio";

        public EntityUid? SelectingSubgroupActionEntity;
        public EntProtoId SelectingSubgroupAction = "VampireSelectingSubgroupAction";

        [AutoNetworkedField]
        public float BloodDamage;

        [AutoNetworkedField]
        public TimeSpan NextBloodDecayDisguise = TimeSpan.Zero;

        [AutoNetworkedField]
        public bool VampireIsBat = false;

        [AutoNetworkedField]
        public bool VampireIsBlood = false;

        [AutoNetworkedField]
        public TimeSpan NextBloodshed = TimeSpan.Zero;

        /// <summary>
        /// активна ли маскировка (игрок не может одновременно находиться в инвизе, быть летучей мышью/призраком)
        /// </summary>
        [AutoNetworkedField]
        public bool DisguiseIsActive = false;

        [AutoNetworkedField]
        public List<EntityUid> GrantedActions = new();

        [AutoNetworkedField]
        public VampireAbilityType SelectedSubgroup;

        [AutoNetworkedField]
        public TimeSpan ClawDurationActive;

        /// <summary>
        /// сколько всего крови выпил вампир
        /// </summary>
        [AutoNetworkedField]
        public float TotalDrunk = 0;

        [AutoNetworkedField]
        public List<int> UnlockedAbilityIndices = new();

        [AutoNetworkedField]
        public bool InvisibleCloneIsActive = false;

        /// <summary>
        /// выбрана ли подгруппа?
        /// </summary>
        [AutoNetworkedField]
        public bool DirectionSelected = false;

        [AutoNetworkedField]
        public EntityUid SleepUid = EntityUid.Invalid;

        [AutoNetworkedField]
        public HashSet<EntityUid> Ghouls = new();

        [AutoNetworkedField]
        public int GhoulQuantity = 0;

        [AutoNetworkedField]
        public bool InvisibleIsActive = false;

        /// <summary>
        /// для OnInvisible
        /// </summary>
        [AutoNetworkedField]
        public bool VampireCloneIsActive = false;

        [AutoNetworkedField]
        public EntityUid VampireUid;

        [AutoNetworkedField]
        public bool AnchorCreate = false;

        [AutoNetworkedField]
        public EntityCoordinates SpawnLocation;

        [AutoNetworkedField]
        public TimeSpan AnchorDurationActive;

        [AutoNetworkedField]
        public EntityUid VampireAnchorUid;

        [AutoNetworkedField]
        public bool InvisibilityAbilityActive = false;

        public Dictionary<VampireAbilityType, string> VampireAbilitiesID = new()
        {
            { VampireAbilityType.Base, "VampireBaseAbilities" },
            { VampireAbilityType.Hemomancer, "VampireHemomancer" },
            { VampireAbilityType.Umbrae, "VampireUmbrae" },
            { VampireAbilityType.Gargantua, "VampireGargantua" }
        };

        [DataField]
        public string MindRoleVampireID = "MindRoleVampire";

        /// <summary>
        /// был ли обращен вампир?
        /// </summary>
        [AutoNetworkedField]
        public bool VampireTurned = false;
    }
}
