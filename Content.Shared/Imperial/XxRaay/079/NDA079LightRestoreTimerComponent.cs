using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Imperial.XxRaay.Nda079;

/// <summary>
/// Temporary component to track when a light should be restored to its original state.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class NDA079LightRestoreTimerComponent : Component
{
    /// <summary>
    /// Time when the light should be restored.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan RestoreTime;

    /// <summary>
    /// Whether the light was enabled before being disabled.
    /// Used to restore the original state.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool WasEnabled;
}

