using Content.Shared.Actions;
using Robust.Shared.Prototypes;

namespace Content.Shared.Imperial.Vampire
{
    /// <summary>
    /// World-targeted action for summoning vampire tentacles
    /// </summary>
    public sealed partial class VampireTentaclesEvent : WorldTargetActionEvent
    {
        /// <summary>
        /// The ID of the entity that is spawned.
        /// </summary>
        [DataField]
        public EntProtoId EntityId = "EffectVampireSpawn";

        /// <summary>
        /// Directions determining where the entities will spawn.
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
        /// How many entities will spawn beyond the original one at the target location?
        /// </summary>
        [DataField]
        public int ExtraSpawns = 4;

        [DataField]
        public int Damage = 15;
    }
}
