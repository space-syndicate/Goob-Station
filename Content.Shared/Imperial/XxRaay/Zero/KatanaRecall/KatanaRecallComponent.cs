using Robust.Shared.GameStates;

namespace Content.Shared.Imperial.XxRaay.Zero.KatanaRecall;

/// <summary>
/// Component that allows KatanaZero to be recalled to its owner's hand.
/// Works similar to ninja katana recall system with instant teleportation.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedKatanaRecallSystem))]
public sealed partial class KatanaRecallComponent : Component
{
    /// <summary>
    /// Maximum distance the katana can be recalled from.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MaxRecallDistance = 30.0f;

    /// <summary>
    /// Cooldown time in seconds between recall attempts.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float RecallCooldown = 3.0f;

    /// <summary>
    /// Time when the katana was last recalled.
    /// </summary>
    [AutoNetworkedField]
    public TimeSpan? LastRecallTime;

    /// <summary>
    /// The action entity that was created for this component.
    /// </summary>
    public EntityUid? ActionEntity;
}
