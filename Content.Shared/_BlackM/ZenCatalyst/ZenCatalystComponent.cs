using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._BlackM.ZenCatalyst;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ZenCatalystComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan SpawnInterval = TimeSpan.FromMinutes(10);

    [DataField, AutoNetworkedField]
    public int SpawnAmount = 3;

    [DataField, AutoNetworkedField]
    public List<EntProtoId> ZenMobPrototypes = new()
    {
        "MobXenInfected",
    };

    [DataField]
    public int MaxPlacementAttempts = 20;

    [DataField, AutoNetworkedField]
    public TimeSpan NextSpawnTime = TimeSpan.Zero;

    [DataField, AutoNetworkedField]
    public bool Enabled = true;
}
