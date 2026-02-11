using Robust.Shared.Map;
using Robust.Shared.Audio;
using Content.Shared.Chat.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Content.Shared.Damage;
using Robust.Shared.Prototypes;
namespace Content.Server.Imperial.SCP.ChaseMusic.Components;

[RegisterComponent]
public sealed partial class ImperialSCPChaseMusicComponent : Component
{
    [ViewVariables, DataField]
    public SoundSpecifier ChaseSound = new SoundPathSpecifier("/Audio/Imperial/Seriozha/SCP/chase/chase1.ogg");
    [ViewVariables, DataField]
    public EntProtoId ChaseMusicToggleAction = "ActionSCPChaseMusic";
    public EntityUid? ChaseMusicToggleActionEntity;

    [ViewVariables]
    public EntityUid? PlayingStream;

    [ViewVariables]
    public bool IsPlaying = false;
}
