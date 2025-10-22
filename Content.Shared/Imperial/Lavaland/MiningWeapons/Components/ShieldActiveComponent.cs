using Robust.Shared.Utility;
using Robust.Shared.GameStates;

namespace Content.Shared.Imperial.Lavaland.MiningWeapons.Components;

[AutoGenerateComponentState]
[RegisterComponent, NetworkedComponent]
public sealed partial class ShieldActiveComponent : Component
{
    [DataField, AutoNetworkedField]
    public SpriteSpecifier? Effect;

    [ViewVariables, AutoNetworkedField]
    public EntityUid SmasherUid;

    [ViewVariables, AutoNetworkedField]
    public EntityUid? UserUid;

    /// <summary>
    /// When will the shield end
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public TimeSpan EndTime;
}
