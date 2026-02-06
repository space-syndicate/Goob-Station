using Robust.Shared.Utility;
using Robust.Shared.GameStates;
using Robust.Shared.Audio;
using Content.Shared.Damage;

namespace Content.Shared.Imperial.MiningWeapons.Smasher.Components;

[AutoGenerateComponentState]
[RegisterComponent, NetworkedComponent]
public sealed partial class ShieldActiveComponent : Component
{
    [DataField]
    [ViewVariables, AutoNetworkedField]
    public DamageModifierSet PassiveBlockDamageModifer = default!;

    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan? TimeDecay;

    [AutoNetworkedField]
    public SpriteSpecifier? EffectActived;

    [AutoNetworkedField]
    public SpriteSpecifier? EffectCharging;

    [AutoNetworkedField]
    public SpriteSpecifier? EffectDecay;

    [ViewVariables, AutoNetworkedField]
    public EntityUid SmasherUid;

    [ViewVariables, AutoNetworkedField]
    public EntityUid? UserUid;

    [ViewVariables, AutoNetworkedField]
    public TimeSpan EndTime;
}
