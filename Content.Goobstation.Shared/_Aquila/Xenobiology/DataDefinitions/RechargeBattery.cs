using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared._Aquila.Xenobiology.DataDefinitions;

public sealed partial class RechargeBattery : EntityEffectBase<RechargeBattery>
{
    /// <summary>
    /// Доля максимального заряда, которая восстанавливается.
    /// </summary>
    [DataField]
    public float Fraction = 0.5f;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;
}
