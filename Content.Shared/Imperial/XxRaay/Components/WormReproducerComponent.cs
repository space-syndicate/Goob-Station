using Content.Shared.Damage.Prototypes;
using Content.Shared.Imperial.XxRaay.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using System.Collections.Generic;
using System.Numerics;

namespace Content.Shared.Imperial.XxRaay.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedWormReproductionSystem))]
public sealed partial class WormReproducerComponent : Component
{
    [DataField]
    public EntProtoId ReproductionAction = "ActionWormReproduction";

    [DataField, AutoNetworkedField]
    public EntityUid? ReproductionActionEntity;

    [DataField]
    public int BloodCost = 300;

    [DataField]
    public TimeSpan WeaveDelay = TimeSpan.FromSeconds(3);

    [DataField]
    public TimeSpan ReproductionDuration = TimeSpan.FromSeconds(120);

    [DataField]
    public ProtoId<DamageTypePrototype> FailDeathDamageType = "Blunt";

    [DataField]
    public EntProtoId CocoonProto;

    [DataField]
    public EntProtoId OffspringProto;

    [DataField]
    public int OffspringCount = 3;

    [DataField]
    public List<Vector2> OffspringOffsets = new()
    {
        new Vector2(0.35f, 0),
        new Vector2(-0.35f, 0),
        new Vector2(0, 0.35f),
    };

    [DataField]
    public EntProtoId ParentResultProto;

    [DataField]
    public EntProtoId SourceProto;
}
