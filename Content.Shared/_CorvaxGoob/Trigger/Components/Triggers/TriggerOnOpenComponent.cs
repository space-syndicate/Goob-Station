using Robust.Shared.GameStates;
using Content.Shared.Trigger.Components.Triggers;

namespace Content.Shared._CorvaxGoob.Trigger.Components.Triggers;

/// <summary>
/// Trigger on open storages
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnOpenComponent : BaseTriggerOnXComponent
{
    /// <summary>
    /// Removes this component after the first trigger.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool RemoveOnTrigger = true;
}