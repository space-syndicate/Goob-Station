using Robust.Shared.Audio;
namespace Content.Server.Imperial.PiratesNewHorizon.Trigger.Components;



/// <summary>
///     Triggers bomb if its in hot environment or
///     has contacted with a hot object (lit welder, lighter, etc).
/// </summary>
[RegisterComponent]
public sealed partial class TriggerOnHeatComponent : Component
{
    /// <summary>
    ///     Minimal surrounding gas temperature to trigger bomb.
    ///     Around 100 degrees celsius by default.
    ///     Doesn't affect hot items temperature.
    /// </summary>
    [DataField("activationTemperature")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float ActivationTemperature = 373;

    /// <summary>
    ///     Should bomb be activated by hot items (welders, lighter, etc)?
    /// </summary>
    [DataField("activateHot")]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool ActivateHotItems = true;

    [DataField] public float Delay = 1f;

    /// <summary>
    ///     If not null, a user can use verbs to configure the delay to one of these options.
    /// </summary>
    [DataField] public List<float>? DelayOptions = null;

    /// <summary>
    ///     If not null, this timer will periodically play this sound while active.
    /// </summary>
    [DataField] public SoundSpecifier? BeepSound;

    /// <summary>
    ///     Time before beeping starts. Defaults to a single beep interval. If set to zero, will emit a beep immediately after use.
    /// </summary>
    [DataField] public float? InitialBeepDelay;

    [DataField] public float BeepInterval = 1;
}
