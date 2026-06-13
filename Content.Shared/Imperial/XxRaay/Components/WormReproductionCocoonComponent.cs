using Content.Shared.Alert;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Imperial.XxRaay.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using System.Collections.Generic;
using System.Numerics;

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
    public List<Vector2> OffspringOffsets = new();

    [DataField]
    public EntProtoId ParentResultProto;

    [DataField]
    public EntProtoId SourceProto;

    [DataField]
    public ProtoId<DamageTypePrototype> FailDeathDamageType;

    [DataField, AutoNetworkedField]
    public TimeSpan StartTime;

    [DataField, AutoNetworkedField]
    public TimeSpan EndTime;

    [DataField]
    public ProtoId<AlertPrototype> TimerAlert = "WormCocoonTimer";

    [DataField]
    public bool Completing;
}
