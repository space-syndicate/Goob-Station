using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.Vampire;

[Serializable, NetSerializable]
public sealed class VampireRequestedEuiMessage(VampireAbilityType selection) : EuiMessageBase
{
    public readonly VampireAbilityType Selection = selection;
}
