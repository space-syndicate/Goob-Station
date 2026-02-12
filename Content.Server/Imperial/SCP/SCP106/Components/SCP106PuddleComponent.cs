using Robust.Shared.Map;
using Robust.Shared.Audio;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Damage;
using Robust.Shared.Prototypes;
namespace Content.Server.Imperial.SCP.SCP106.Components;

[RegisterComponent]
public sealed partial class SCP106PuddleComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public MapId? TargetMap;
    [DataField("teleportSound")]
    public SoundSpecifier TeleportSound = new SoundPathSpecifier("/Audio/Imperial/SCP/scp106_teleported_local.ogg");
    [DataField("globalTeleportSound")]
    public SoundSpecifier GlobalTeleportSound = new SoundPathSpecifier("/Audio/Imperial/SCP/scp106_teleported_global.ogg");
    [DataField]
    public ProtoId<EmotePrototype> PrototypeScream = "Scream";
    [DataField(required: true), ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier DamagePerSecond = new();
}
