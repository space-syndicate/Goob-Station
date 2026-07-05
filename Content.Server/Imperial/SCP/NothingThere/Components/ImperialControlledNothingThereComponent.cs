using Robust.Shared.Map;
using Robust.Shared.Audio;
using Content.Shared.Chat.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Content.Shared.Damage;
using Robust.Shared.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Systems;
namespace Content.Server.Imperial.SCP.NothingThere.Components;

[RegisterComponent]
public sealed partial class ImperialControlledNothingThereComponent : Component
{
    [DataField("gibBodyAction")]
    public EntProtoId GibBodyAction = "ImperialNothingThereGibBodyAction";
    [ViewVariables]
    public EntityUid? GibBodyEntity;
    [ViewVariables]
    public EntityUid OriginalBody = EntityUid.Invalid;
    [ViewVariables]
    public SoundSpecifier ExitSound = new SoundPathSpecifier("/Audio/Imperial/SCP/nothingthere_gibbody.ogg");
    [ViewVariables]
    public int KillCount = 0;
    [ViewVariables]
    public SoundSpecifier GibSound = new SoundCollectionSpecifier("gib", AudioParams.Default.WithVariation(0.025f));
    [ViewVariables]
    public float GibletLaunchImpulse = 150;
    [ViewVariables]
    public float GibletLaunchImpulseVariance = 3;
}
