using System.Numerics;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Decals;
using Robust.Shared.Prototypes;

namespace Content.Server.Imperial.BloodTrail
{
    [RegisterComponent]
    public sealed partial class BloodTrailComponent : Component
    {
        [DataField] public int MaxDecals = 20;
        [DataField] public float SpreadDistance = 1.0f;
        [DataField] public TimeSpan SpawnCooldown = TimeSpan.FromSeconds(0.5f);
        [DataField] public bool Enabled = true;

        [ViewVariables] public int CurrentDecalCount;
        [ViewVariables] public TimeSpan NextSpawnTime;
        [ViewVariables] public Dictionary<Vector2, TimeSpan> RecentDecalPositions = new();

        [DataField("decals")]
        public List<ProtoId<DecalPrototype>> Decals = new()
        {
            "bloodtrail1", "bloodtrail2", "bloodtrail3",
            "bloodtrail4", "bloodtrail5", "bloodtrail6"
        };

        [DataField] public HashSet<ProtoId<DamageGroupPrototype>> DamageGroups = new() { "Brute" };

        [DataField]
        public HashSet<ProtoId<DamageTypePrototype>> DamageTypes = new()
        {
            "Blunt", "Slash", "Piercing"
        };

        [DataField]
        public Dictionary<ProtoId<DamageTypePrototype>, float> DamageTypeModifiers = new()
        {
            ["Blunt"] = 0.2f,
            ["Slash"] = 1.0f,
            ["Piercing"] = 0.8f
        };
    }
}
