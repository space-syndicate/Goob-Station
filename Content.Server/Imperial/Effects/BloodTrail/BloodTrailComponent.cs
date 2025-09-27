using Content.Shared.Decals;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Server.Imperial.BloodTrail
{
    [RegisterComponent]
    public sealed partial class BloodTrailComponent : Component
    {
        [DataField] public FixedPoint2 MinDamageToSpawn = FixedPoint2.New(5f);
        [DataField] public int MaxDecals = 20;
        [DataField] public float SpreadDistance = 0.3f;
        [DataField] public TimeSpan SpawnCooldown = TimeSpan.FromSeconds(0.5f);
        [DataField] public bool Enabled = true;

        [ViewVariables] public int CurrentDecalCount;
        [ViewVariables] public TimeSpan NextSpawnTime;

        [DataField("decals")]
        public List<ProtoId<DecalPrototype>> Decals = new()
        {
            "bloodtrail1", "bloodtrail2", "bloodtrail3",
            "bloodtrail4", "bloodtrail5", "bloodtrail6", "bloodtrail7"
        };

        [DataField] public HashSet<string> DamageGroups = new() { "Brute" };

        [DataField]
        public HashSet<string> DamageTypes = new()
        {
            "Blunt", "Slash", "Piercing"
        };

        [DataField]
        public Dictionary<string, float> DamageTypeModifiers = new()
        {
            ["Blunt"] = 0.2f,
            ["Slash"] = 1.0f,
            ["Piercing"] = 0.8f
        };
    }
}
