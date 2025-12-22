using Content.Shared.Eui;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.Vampire;

[Serializable, NetSerializable]
public sealed class VampireRequestedEuiMessage(int selectionNumber) : EuiMessageBase
{
    public readonly int SelectionNumber = selectionNumber;
}
