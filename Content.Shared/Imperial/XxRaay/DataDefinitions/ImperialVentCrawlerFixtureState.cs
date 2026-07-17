using Robust.Shared.Serialization;
using System;
using Content.Shared.Physics;

namespace Content.Shared.Imperial.XxRaay.DataDefinitions;

[DataDefinition, NetSerializable, Serializable]
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


