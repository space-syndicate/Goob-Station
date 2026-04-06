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
    [AutoNetworkedField, DataField("gibBodyEntity")]
    public EntityUid? GibBodyEntity;

    public EntityUid OriginalBody = EntityUid.Invalid;
    [DataField("exitSound")]
    public SoundSpecifier ExitSound = new SoundPathSpecifier("/Audio/Imperial/SCP/nothingthere_gibbody.ogg");
    [DataField("killCount")]
    public int KillCount = 0;
}
