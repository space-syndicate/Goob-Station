using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.GameStates;

namespace Content.Shared._CorvaxGoob.Trigger.Components.Triggers;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class StaminaDrainOnTriggerComponent : BaseXOnTriggerComponent
{
    [DataField, AutoNetworkedField]
    public float Radius = 3f;

    [DataField, AutoNetworkedField]
    public float StaminaDamagePerSecond = 25f;
}
