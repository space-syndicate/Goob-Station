using Content.Shared.Imperial.XxRaay.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Imperial.XxRaay.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedWormReproductionSystem))]
public sealed partial class WormReproductionCocoonComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid ParentWorm;

    [DataField]
    public EntProtoId OffspringProto;

    [DataField]
    public int OffspringCount = 3;

    [DataField]
    public EntProtoId ParentResultProto;

    [DataField]
    public EntProtoId SourceProto;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField]
    public TimeSpan EndTime;
}
