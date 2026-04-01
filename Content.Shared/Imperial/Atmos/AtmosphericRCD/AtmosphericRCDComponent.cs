using Content.Shared.RCD.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;
using Content.Shared.Tag;
using Content.Shared.Imperial.Atmospheric.RCD.Systems;

namespace Content.Shared.Imperial.Atmospheric.RCD.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(AtmosphericRCDSystem))]
public sealed partial class AtmosphericRCDComponent : Component
{
    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<AtmosphericRCDPrototype>> AvailablePrototypes { get; set; } = new();

    [DataField]
    public SoundSpecifier SuccessSound { get; set; } = new SoundPathSpecifier("/Audio/Items/deconstruct.ogg");

    [DataField, AutoNetworkedField]
    public ProtoId<AtmosphericRCDPrototype> ProtoId { get; set; } = "Invalid";

    [DataField, AutoNetworkedField]
    public Direction ConstructionDirection
    {
        get => _constructionDirection;
        set
        {
            _constructionDirection = value;
        }
    }

    [ViewVariables(VVAccess.ReadOnly)]
    private Direction _constructionDirection;

    [ViewVariables(VVAccess.ReadOnly)]
    public Transform ConstructionTransform => new Transform(new(), ConstructionDirection.ToAngle());

    [ViewVariables(VVAccess.ReadOnly)]
    public int InstantConstructionDelay = 0;
    [ViewVariables(VVAccess.ReadOnly)]
    public EntProtoId InstantConstructionFx = "EffectRCDConstruct0";
    [ViewVariables(VVAccess.ReadOnly)]
    public ProtoId<AtmosphericRCDPrototype> DeconstructTileProto = "DeconstructTile";
    [ViewVariables(VVAccess.ReadOnly)]
    public ProtoId<AtmosphericRCDPrototype> DeconstructLatticeProto = "DeconstructLattice";
    [ViewVariables(VVAccess.ReadOnly)]
    public ProtoId<TagPrototype> CatwalkTag = "Catwalk";
    [ViewVariables(VVAccess.ReadOnly)]
    public HashSet<EntityUid> IntersectingEntities = new();
}
