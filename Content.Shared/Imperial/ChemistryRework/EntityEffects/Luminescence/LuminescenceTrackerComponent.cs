using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Chemistry.ReactionEffects;


/// <summary>
/// Keeps point light alive while a luminescence effect is active; removed after the grace period expires.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class LuminescenceTrackerComponent : Component
{
    [DataField]
    public float Accumulated = 0f;

    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan GracePeriod = TimeSpan.FromSeconds(1);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextCheck = TimeSpan.Zero;
}
