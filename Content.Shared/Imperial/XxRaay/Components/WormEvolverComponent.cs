using Content.Shared.Damage.Prototypes;
using Content.Shared.Imperial.XxRaay.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Imperial.XxRaay.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedWormEvolutionSystem))]
public sealed partial class WormEvolverComponent : Component
{
    [DataField]
    public EntProtoId EvolutionAction = "ActionWormEvolution";

    [DataField, AutoNetworkedField]
    public EntityUid? EvolutionActionEntity;

    [DataField]
    public int BloodCost = 100;

    [DataField]
    public TimeSpan WeaveDelay = TimeSpan.FromSeconds(3);

    [DataField]
    public TimeSpan EvolutionDuration = TimeSpan.FromSeconds(90);

    [DataField]
    public ProtoId<DamageTypePrototype> FailDeathDamageType = "Blunt";

    [DataField]
    public EntProtoId CocoonProto;

    [DataField]
    public EntProtoId ResultProto;

    [DataField]
    public EntProtoId SourceProto;
}
