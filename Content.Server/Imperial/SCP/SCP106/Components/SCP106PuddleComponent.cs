using Robust.Shared.Map;
using Robust.Shared.Audio;
using Content.Shared.Chat.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Content.Shared.Damage;
namespace Content.Server.Imperial.SCP.SCP106.Components;

[RegisterComponent]
public sealed partial class SCP106PuddleComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public MapId TargetMap { get; internal set; }
    [DataField("teleportSound")]
    public SoundSpecifier TeleportSound = new SoundPathSpecifier("/Audio/Imperial/SCP/scp106_teleported_local.ogg");
    [DataField("globalTeleportSound")]
    public SoundSpecifier GlobalTeleportSound = new SoundPathSpecifier("/Audio/Imperial/SCP/scp106_teleported_global.ogg");
    [DataField("prototypeScream", customTypeSerializer: typeof(PrototypeIdSerializer<EmotePrototype>))]
    public string PrototypeScream = "Scream";
    [DataField(required: true), ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier DamagePerSecond = new();
}