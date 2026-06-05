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
    [RegisterComponent, AutoGenerateComponentState, NetworkedComponent]
    public sealed partial class VampireComponent : Component
    {
        [AutoNetworkedField]
        public float BloodDamage;

        /// <summary>
        /// стартовое количество крови у вампира
        /// </summary>
        public float CritThreshold = 100f;

        [AutoNetworkedField]
        public HashSet<EntityUid> Ghouls = new();

        /// <summary>
        /// сколько занимает излечение вампира
        /// </summary>
        [DataField]
        public TimeSpan VampireCure = TimeSpan.FromSeconds(15);

        [DataField]
        public string CooldownStatusEffectAppealGhouls = "AppealGhoulsCooldown";

        [AutoNetworkedField]
        public int GhoulQuantity = 0;

        /// <summary>
        /// максимальное количество упырей, которое может иметь вампир
        /// </summary>
        [DataField]
        public int MaxNumberGhouls = 5;

        public Dictionary<VampireAbilityType, string> VampireAbilitiesID = new()
        {
            { VampireAbilityType.Base, "VampireBaseAbilities" },
            { VampireAbilityType.Hemomancer, "VampireHemomancer" },
            { VampireAbilityType.Umbrae, "VampireUmbrae" },
            { VampireAbilityType.Gargantua, "VampireGargantua" }
        };

        [AutoNetworkedField]
        public List<int> UnlockedAbilityIndices = new();

        [AutoNetworkedField]
        public List<EntityUid> GrantedActions = new();

        /// <summary>
        /// выбрана ли подгруппа?
        /// </summary>
        [AutoNetworkedField]
        public bool DirectionSelected = false;

        public EntityUid? SelectingSubgroupActionEntity;

        /// <summary>
        /// сколько длится doAfter перед обращением игрока в упыря
        /// </summary>
        [DataField("conversionGhoulTime")]
        public TimeSpan ConversionGhoulTime = TimeSpan.FromSeconds(5);

        /// <summary>
        /// количество выпитой крови за 1 тик
        /// </summary>
        [DataField("bloodPerTick")]
        public float BloodPerTick = 3;

        /// <summary>
        /// сколько всего крови выпил вампир
        /// </summary>
        [AutoNetworkedField]
        public float TotalDrunk = 0;

        /// <summary>
        /// звук питья крови
        /// </summary>
        [DataField("drinkSound")]
        public SoundSpecifier DrinkSound = new SoundPathSpecifier("/Audio/Items/drink.ogg")
        {
            Params = AudioParams.Default.WithVolume(3)
        };

        [DataField]
        public ProtoId<FactionIconPrototype> StatusIcon = "VampireFactionAction";

        [AutoNetworkedField]
        public VampireAbilityType SelectedSubgroup;
    }
}
