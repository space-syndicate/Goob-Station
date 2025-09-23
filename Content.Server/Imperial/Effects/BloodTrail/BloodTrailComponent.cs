using Content.Shared.Decals;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server.Imperial.BloodTrail
{
    [RegisterComponent]
    public sealed partial class BloodTrailComponent : Component
    {
        [DataField("minDamageToSpawn")]
        public float MinDamageToSpawn = 5f;

        [DataField("maxDecals")]
        public int MaxDecals = 20;

        [DataField("spreadDistance")]
        public float SpreadDistance = 0.3f;

        [ViewVariables]
        public int CurrentDecalCount = 0;

        [DataField("spawnCooldown")]
        public TimeSpan SpawnCooldown = TimeSpan.FromSeconds(0.5f);

        [ViewVariables(VVAccess.ReadWrite)]
        public TimeSpan NextSpawnTime = TimeSpan.Zero;

        [DataField("decals", customTypeSerializer: typeof(PrototypeIdListSerializer<DecalPrototype>))]
        public List<string> Decals = new()
        {
            "bloodtrail1",
            "bloodtrail2",
            "bloodtrail3",
            "bloodtrail4",
            "bloodtrail5",
            "bloodtrail6",
            "bloodtrail7"
        };

        [DataField("damageGroups")]
        public HashSet<string> DamageGroups = new()
        {
            "Brute"
        };

        [DataField("damageTypes")]
        public HashSet<string> DamageTypes = new()
        {
            "Blunt",
            "Slash",
            "Piercing"
        };

        [DataField("damageTypeChances")]
        public Dictionary<string, float> DamageTypeChances = new()
        {
            ["Blunt"] = 0.3f,
            ["Slash"] = 0.9f,
            ["Piercing"] = 0.8f,
        };

        [DataField("bloodColor")]
        public Color BloodColor = Color.DarkRed;

        [DataField("enabled")]
        public bool Enabled = true;
    }
}
