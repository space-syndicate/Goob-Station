using Robust.Shared.GameStates;
using Content.Shared.Damage;
using Robust.Shared.Audio;

namespace Content.Shared.Imperial.Damage;

/// <summary>
/// When added to a USER, it immediately blocks the damage passively dealt to them.
/// </summary>
[AutoGenerateComponentState]
[RegisterComponent, NetworkedComponent]
public sealed partial class ImperialShieldComponent : Component
{
    [DataField]
    [ViewVariables, AutoNetworkedField]
    public DamageModifierSet PassiveBlockDamageModifer = default!;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool HasBlockSound = false;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier BlockSound = new SoundPathSpecifier("/Audio/Weapons/block_metal1.ogg");
}
