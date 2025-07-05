using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.ImperialBorgs.Events;

[Serializable, NetSerializable]
public sealed class BorgHypoComponentState(bool uiUpdateNeeded, string currentReagenName) : ComponentState
{
    public readonly bool UiUpdateNeeded = uiUpdateNeeded;
    public readonly string CurrentReagentName = currentReagenName;
}
