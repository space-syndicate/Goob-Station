using Content.Shared.Physics;

namespace Content.Server.Imperial.XxRaay.DataDefinitions;

[DataDefinition]
public sealed partial class ImperialVentCrawlerFixtureState
{
    [DataField]
    public string Id = string.Empty;

    [DataField]
    public bool Hard;

    [DataField]
    public CollisionGroup CollisionLayer;

    [DataField]
    public CollisionGroup CollisionMask;
}
