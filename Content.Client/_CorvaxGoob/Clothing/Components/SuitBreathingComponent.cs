using Robust.Shared.Audio;

namespace Content.Client._CorvaxGoob.Clothing.Components;

[RegisterComponent]
public sealed partial class SuitBreathingComponent : Component
{
    [DataField]
    public SoundSpecifier? BreathingSound = new SoundCollectionSpecifier("SuitBreathingSounds", AudioParams.Default.WithVolume(-6f));

    [DataField]
    public TimeSpan MinInterval = TimeSpan.FromSeconds(3.5);

    [DataField]
    public TimeSpan MaxInterval = TimeSpan.FromSeconds(5.0);

    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan Timer;
}
