using Content.Server.Objectives.Systems;
using Content.Server.Imperial.NinjaMultitask.Systems;
namespace Content.Server.Imperial.NinjaMultitask.Components;

[RegisterComponent, Access(typeof(DealDamageConditionSystem))]
public sealed partial class NinjaDamageTargetComponent : Component
{
    [DataField]
    public EntityUid? Objective;
}