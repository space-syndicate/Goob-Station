using Content.Shared.Doors.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Imperial.XxRaay.Nda079;

/// <summary>
/// Temporary component to track when a door bolt should be unbolted.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class NDA079AirlockBoltTimerComponent : Component
{
    /// <summary>
    /// Time when the bolt should be unbolted.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan UnboltTime;

    /// <summary>
    /// Whether the door was bolted before this component was added.
    /// Used to restore the original state.
    /// </summary>
    [DataField]
    public bool WasBolted = true;
}

