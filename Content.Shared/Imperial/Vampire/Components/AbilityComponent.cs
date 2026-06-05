using Content.Shared.Alert;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.StatusIcon;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared.Imperial.Vampire
{
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    public sealed partial class AbilityComponent : Component
    {
        /// <summary>
        /// ID сущности когтя вампира
        /// </summary>
        [DataField("swordId")]
        public EntProtoId SwordId = "VampireSword";

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
        /// кд между призывами катаны
        /// </summary>
        [AutoNetworkedField]
        public TimeSpan CooldownSword = TimeSpan.FromSeconds(30);

        /// <summary>
        /// кд между созданиями кровавого якоря
        /// </summary>
        [DataField("cooldownBloodAnchor")]
        public TimeSpan CooldownBloodAnchor = TimeSpan.FromSeconds(130);

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

        /// <summary>
        /// время следующего тика потери крови
        /// </summary>
        public TimeSpan NextBloodDecay = TimeSpan.Zero;

        /// <summary>
        /// Интервал между тиками потери крови
        /// </summary>
        [DataField]
        public TimeSpan BloodDecayInterval = TimeSpan.FromSeconds(45);

        /// <summary>
        /// количество урона за каждый тик
        /// </summary>
        [DataField]
        public float BloodDecayAmount = 2f;

        [DataField]
        public EntProtoId GhoulPuddleID = "VampirePuddle";

        /// <summary>
        /// длительность тряски при критическом состоянии
        /// </summary>
        [DataField]
        public TimeSpan ShakingTime = TimeSpan.FromSeconds(5);

        [DataField]
        public string VampirePuddleID = "VampirePuddle";

        [DataField]
        public string VampireRadioID = "VampireRadio";

        public EntProtoId SelectingSubgroupAction = "VampireSelectingSubgroupAction";

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
        public bool InvisibleIsActive = false;

        [AutoNetworkedField]
        public TimeSpan ClawDurationActive;

        [AutoNetworkedField]
        public bool InvisibleCloneIsActive = false;

        [AutoNetworkedField]
        public EntityUid SleepUid = EntityUid.Invalid;

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

        [AutoNetworkedField]
        public List<EntityUid> BatsUid = new();

        [DataField]
        public string MindRoleVampireID = "MindRoleVampire";

        [AutoNetworkedField]
        public EntityUid? CloneUid;

        [AutoNetworkedField]
        public TimeSpan FlashEffectDuration;

        /// <summary>
        /// был ли обращен вампир?
        /// </summary>
        [AutoNetworkedField]
        public bool VampireTurned = false;

        /// <summary>
        /// длительность cooldown на обращение в упырей
        /// </summary>
        [DataField("cooldownTimeAppealGhouls")]
        public TimeSpan CooldownTimeAppealGhouls = TimeSpan.FromMinutes(2.5f);

        [DataField]
        public DamageSpecifier DivineDamage = new DamageSpecifier
        {
            DamageDict = new Dictionary<ProtoId<DamageTypePrototype>, FixedPoint2>
            {
                ["Heat"] = 2
            }
        };

        [DataField]
        public SoundSpecifier DivineDamageSound = new SoundPathSpecifier("/Audio/Effects/lightburn.ogg")
        {
            Params = AudioParams.Default.WithVolume(3)
        };

        [AutoNetworkedField]
        public float UpdateDelay;

        [DataField]
        public ProtoId<AlertPrototype> AdjacentChaplainAlert = "VampireAdjacentChaplainAlert";

        [DataField]
        public string HaloEffect = "VampireHaloEffect";

        /// <summary>
        /// сколько базовых способностей вампира будет выдано упырям после обращения
        /// </summary>
        [DataField]
        public int GhoulBaseAbility = 2;

        /// <summary>
        /// сколько уникальных способностей вампира будет выдано упырям после обращения
        /// </summary>
        [DataField]
        public int GhoulGroupAbility = 1;

        [AutoNetworkedField]
        public EntityUid? HaloUid;
    }
}
