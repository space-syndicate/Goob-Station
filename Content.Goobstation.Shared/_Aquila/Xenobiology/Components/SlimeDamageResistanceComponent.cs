using Content.Goobstation.Shared._Aquila.Xenobiology.Systems;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared._Aquila.Xenobiology.Components;

[RegisterComponent, NetworkedComponent, Access(typeof(SlimeDamageResistanceSystem))]
public sealed partial class SlimeDamageResistanceComponent : Component
{
    /// <summary>
    /// Набор модификаторов, применяемый ко всему входящему урону.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<DamageModifierSetPrototype> ModifierSet;
}
