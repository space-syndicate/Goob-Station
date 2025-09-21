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
            "trail1",
            "trail2",
            "trail3",
            "trail4",
            "trail5"
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

        [DataField("bloodColor")]
        public Color BloodColor = Color.DarkRed;

        [DataField("enabled")]
        public bool Enabled = true;
    }
}
