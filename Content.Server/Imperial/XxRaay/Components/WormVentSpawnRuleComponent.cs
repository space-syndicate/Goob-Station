using Content.Server.Imperial.XxRaay.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server.Imperial.XxRaay.Components;

[RegisterComponent, Access(typeof(WormVentSpawnRule))]
public sealed partial class WormVentSpawnRuleComponent : Component
{
    [DataField]
    public EntProtoId Prototype = "MobWormTier1";

    [DataField]
    public int Count = 6;
}
