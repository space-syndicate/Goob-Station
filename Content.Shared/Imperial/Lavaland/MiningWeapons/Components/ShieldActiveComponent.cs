using Robust.Shared.Utility;
using Robust.Shared.GameStates;

namespace Content.Shared.Imperial.Lavaland.MiningWeapons.Components;

[AutoGenerateComponentState]
[RegisterComponent, NetworkedComponent]
public sealed partial class ShieldActiveComponent : Component
{
    [DataField, AutoNetworkedField]
    public SpriteSpecifier? EffectActived;

    [DataField, AutoNetworkedField]
    public SpriteSpecifier? EffectCharging;

    [DataField, AutoNetworkedField]
    public SpriteSpecifier? EffectDecay;

    [ViewVariables, AutoNetworkedField]
    public EntityUid SmasherUid;

    [ViewVariables, AutoNetworkedField]
    public EntityUid? UserUid;

    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public TimeSpan EndTime;
}
