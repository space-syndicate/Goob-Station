using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._BlackM.ProjectileTrauma;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[AutoGenerateComponentPause(Dirty = true)]
public sealed partial class ProjectileTraumaComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan HoldUntil;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan RecoverUntil;

    [DataField, AutoNetworkedField]
    public float WalkModifier = 1f;

    [DataField, AutoNetworkedField]
    public float SprintModifier = 1f;

    public float LastWalkModifier = 1f;
    public float LastSprintModifier = 1f;
}
