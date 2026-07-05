using Content.Shared.Alert;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Imperial.XxRaay.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

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
