using Content.Shared.Chat.Prototypes;
using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CorvaxGoob.Trigger.Components.Triggers;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class EmoteOnTriggerComponent : BaseXOnTriggerComponent
{
    [DataField, AutoNetworkedField]
    public float Radius = 3f;

    [DataField, AutoNetworkedField]
    public ProtoId<EmotePrototype> Emote = "DanceFromTheFloor";
}
