using Robust.Shared.Utility;
using Robust.Shared.GameStates;
using Robust.Shared.Audio;
using Content.Shared.Damage;

namespace Content.Shared.Imperial.Lavaland.MiningWeapons.Smasher.Components;

[AutoGenerateComponentState]
[RegisterComponent, NetworkedComponent]
public sealed partial class ShieldActiveComponent : Component
{
    [DataField]
    [ViewVariables, AutoNetworkedField]
    public DamageModifierSet PassiveBlockDamageModifer = default!;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier BlockSound = new SoundPathSpecifier("/Audio/Weapons/block_metal1.ogg");

    [ViewVariables, AutoNetworkedField]
    public Dictionary<string, float> DamageBlockedCoefficients = new()
    {
        ["Blunt"] = 0.1f,
        ["Slash"] = 0.1f,
        ["Piercing"] = 0.10f,
        ["Heat"] = 0.1f
    };

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
