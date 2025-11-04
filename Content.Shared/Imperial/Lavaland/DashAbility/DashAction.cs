using Content.Shared.Actions;

namespace Content.Shared.Imperial.Abilities.Urs;

public sealed partial class UrsDashAction : EntityTargetActionEvent
{
    [DataField]
    public TimeSpan StunTime = TimeSpan.FromSeconds(2f);
}
