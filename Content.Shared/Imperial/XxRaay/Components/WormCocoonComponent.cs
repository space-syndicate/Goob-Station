using Content.Shared.Imperial.XxRaay.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Imperial.XxRaay.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedWormEvolutionSystem))]
public sealed partial class WormCocoonComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid Worm;

    [DataField]
    public EntProtoId ResultProto;

    [DataField]
    public EntProtoId SourceProto;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField]
    public TimeSpan EndTime;
}
