using Content.Shared.Actions;
using Robust.Shared.Prototypes;

namespace Content.Shared.Imperial.Vampire
{
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
        [DataField]
        public int ExtraSpawns = 4;

        [DataField("costBlood")]
        public float CostBlood = 30;
    }
}
