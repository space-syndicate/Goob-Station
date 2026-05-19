using Content.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Imperial.Atmospheric.RCD;

/// <summary>
/// Contains the parameters for an RCD construction / operation
/// </summary>
[Prototype("atmosphericRcd")]
public sealed partial class AtmosphericRCDPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true), ViewVariables(VVAccess.ReadOnly)]
    public AtmosphericRcdMode Mode { get; private set; } = AtmosphericRcdMode.Invalid;

    [DataField("name"), ViewVariables(VVAccess.ReadOnly)]
    public string SetName { get; private set; } = "Unknown";

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public string Category { get; private set; } = "Undefined";

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public SpriteSpecifier? Sprite { get; private set; }

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public EntProtoId? Prototype { get; private set; }

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public int Cost { get; private set; } = 1;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public float Delay { get; private set; } = 1f;

    [DataField("fx"), ViewVariables(VVAccess.ReadOnly)]
    public EntProtoId? Effect { get; private set; }

    [DataField("rules"), ViewVariables(VVAccess.ReadOnly)]
    public HashSet<AtmosphericRcdConstructionRule> ConstructionRules { get; private set; } = new();

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public CollisionGroup CollisionMask { get; private set; } = CollisionGroup.None;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public Box2? CollisionBounds
    {
        get => _collisionBounds;

        private set
        {
            _collisionBounds = value;

            if (_collisionBounds != null)
            {
                var poly = new PolygonShape();
                poly.SetAsBox(_collisionBounds.Value);

                CollisionPolygon = poly;
            }
        }
    }

    private Box2? _collisionBounds;

    [ViewVariables(VVAccess.ReadOnly)]
    public PolygonShape? CollisionPolygon { get; private set; }

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public AtmosphericRcdRotation AtmosphericRotation { get; private set; } = AtmosphericRcdRotation.User;
}

public enum AtmosphericRcdMode : byte
{
    Invalid,
    Deconstruct,
    ConstructTile,
    ConstructObject,
}

public enum AtmosphericRcdConstructionRule : byte
{
    MustBuildOnEmptyTile,
    CanBuildOnEmptyTile,
    MustBuildOnSubfloor,
    IsWindow,
    IsCatwalk,
}

public enum AtmosphericRcdRotation : byte
{
    Fixed,
    Camera,
    User,
}
