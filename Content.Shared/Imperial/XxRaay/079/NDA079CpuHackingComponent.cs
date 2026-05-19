using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Imperial.XxRaay.Nda079;

/// <summary>
/// Temporary component to track CPU hacking progress messages.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class NDA079CpuHackingComponent : Component
{
    /// <summary>
    /// Current progress percentage (0-100).
    /// </summary>
    [DataField, AutoNetworkedField]
    public int CurrentPercent = 0;

    /// <summary>
    /// Time when the next progress message should be sent.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextMessageTime;

    /// <summary>
    /// The level that is being hacked.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int TargetLevel;
}

