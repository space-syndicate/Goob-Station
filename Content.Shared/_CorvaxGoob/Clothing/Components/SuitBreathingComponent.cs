using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared._CorvaxGoob.Clothing;

[RegisterComponent]
public sealed partial class SuitBreathingComponent : Component
{
    [DataField("breathingSound")]
    public SoundSpecifier? BreathingSound = new SoundCollectionSpecifier("SuitBreathingSounds", AudioParams.Default.WithVolume(-6f));

    [DataField("minInterval")]
    public float MinInterval = 3.5f;

    [DataField("maxInterval")]
    public float MaxInterval = 5.0f;

    [ViewVariables(VVAccess.ReadWrite)]
    public float Timer = 0f;
}
