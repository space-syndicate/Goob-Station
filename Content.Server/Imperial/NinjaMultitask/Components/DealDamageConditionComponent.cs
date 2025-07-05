using Content.Server.Objectives.Systems;
using Content.Server.Imperial.NinjaMultitask.Systems;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Network;
namespace Content.Server.Imperial.NinjaMultitask.Components;

/// <summary>
/// Requires that the player does some certain amount of damage to play for the objective to be completed
/// </summary>
[RegisterComponent, Access(typeof(DealDamageConditionSystem))]
public sealed partial class DealDamageConditionComponent : Component
{
    [DataField]
    public FixedPoint2 MinDamage = 100;
    [DataField]
    public FixedPoint2 MaxDamage = 150;
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<DamageTypePrototype>? DamageType;
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 DamageDealt = 0;
    [DataField(required: true)]
    public HashSet<ProtoId<DamageTypePrototype>> DamageTypePool = default!;
    [DataField]
    public EntityUid? Ninja;
    [DataField]
    public EntityUid? OriginalBody;
    [DataField(required: true), ViewVariables(VVAccess.ReadWrite)]
    public LocId Title = string.Empty;
    [DataField]
    public bool Failed = false;
    [DataField]
    public bool DealDamageYourself = false;
}
